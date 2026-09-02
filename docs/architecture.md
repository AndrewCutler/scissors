# Architecture

Scissors is a client-server application with several client experiences backed
by one ASP.NET Core API and PostgreSQL database:

```text
Windows desktop app (Avalonia)  ─┐
Mobile app (React Native/Expo)   ├── HTTP API ── PostgreSQL
Web app (React Native Web/Expo)  ┘       └── SignalR hub
```

Clients do not connect directly to the database. Authentication, authorization,
data access, synchronization, and API compatibility are owned by the backend.

## Repository Layout

```text
Scissors.API/          ASP.NET Core backend, EF Core model, handlers, migrations
Scissors.API.Tests/    Backend unit and component tests
Scissors.Desktop/      Windows-only Avalonia desktop application
Scissors.Desktop.Tests/ Desktop unit tests
scissors-mobile/       Expo React Native application for mobile and web
docs/                  Project documentation
```

The API and desktop projects are .NET 10 projects. The mobile/web project is a
TypeScript Expo project. Local backend development can run PostgreSQL and the
API through `docker-compose.yml`.

## Backend API

The backend is an ASP.NET Core application in `Scissors.API`.

### HTTP API

The API uses ASP.NET Core Minimal API endpoint mappings. Endpoints are grouped
under `/api/v1`, with handlers organized by feature:

```text
Handlers/
  Auth/
  Clippings/
```

Handlers are static feature units that receive dependencies through method
parameters. This keeps HTTP concerns close to each use case while allowing the
database and other services to be supplied by dependency injection.

The current API provides:

- Clipping list, create, update, and delete operations
- Google authentication completion for desktop and mobile clients
- Refresh-token flows for desktop, mobile, and web clients
- Logout
- An anonymous `/health` endpoint
- Development-only OpenAPI and Swagger UI

API versioning is enabled with version `1.0` as the default. Breaking API
changes should use a new major API contract rather than silently changing the
behavior expected by existing clients.

### Authentication and Authorization

The API uses JWT bearer authentication. Authorization has a fallback policy
requiring authentication, so endpoints must explicitly opt out with
`AllowAnonymous` when they are intended to be public.

Google OAuth is completed by separate client-specific handlers. Refresh tokens
are associated with users and devices, allowing the backend to distinguish and
manage sessions on different clients.

### Persistence

Entity Framework Core owns the PostgreSQL persistence model through
`Data/ScissorsDbContext.cs`. The current model includes:

- Users
- External Google identities
- Refresh tokens
- Devices
- Clippings

Entities are mapped in the `DbContext`, and API DTOs are used at the HTTP
boundary rather than exposing persistence entities as the long-term public
contract.

Schema changes are represented by EF Core migrations in `Scissors.API/Migrations`.
Production migrations should run as a controlled deployment step before the
new application revision is activated, using a direct database connection when
the provider uses a connection pooler.

Local development uses PostgreSQL in Docker Compose. Production uses a managed
PostgreSQL database, currently Neon.

### Real-Time Synchronization

`Hub/ClippingsHub.cs` exposes a SignalR hub at `/clippingsHub`. The hub requires
authorization and uses the authenticated subject to target updates to the
appropriate user.

Clipping operations can notify connected clients about new, updated, and deleted
clippings. SignalR is a synchronization channel; the HTTP API and database
remain the source of truth. Clients resynchronize through HTTP after reconnects
to recover from missed real-time messages.

### Cross-Cutting Services

The API configures these cross-cutting concerns in `Program.cs`:

- Dependency injection
- Serilog console and rolling-file logging
- Global exception handling and problem details
- CORS policies for desktop, mobile, and web clients
- Health checks
- JWT authentication and authorization
- API versioning
- HTTP client support

Configuration is loaded through .NET configuration providers. Production
secrets should be supplied through the deployment environment rather than
checked into `appsettings.json`.

## Windows Desktop App

The desktop app is an Avalonia application in `Scissors.Desktop`. It currently
targets Windows only through `net10.0-windows10.0.26100.0`.

### UI Pattern

The UI follows Avalonia's MVVM model:

- `Views/` contains Avalonia windows and XAML.
- `ViewModels/` contains presentation state and user actions.
- `Models/` contains client-side models and response DTOs.
- `App.axaml` and `Program.cs` configure application startup.

`MainViewModel` coordinates authentication, clipping operations, local state,
and the real-time connection. The view model does not directly construct HTTP
clients or access the operating system; those responsibilities are delegated
to services.

### Services and Dependency Injection

The desktop app uses interfaces and dependency injection for its application
services, including:

- `IScissorsApiClient` for HTTP API calls
- `IClippingService` for clipping use cases
- `IClippingStore` for in-memory clipping state
- `IClippingHubConnectionService` for SignalR
- `IAuthTokenRefreshService` for access-token renewal
- `IDeviceStorage` and `IRefreshTokenStore` for local persistence

This separation keeps platform-specific behavior replaceable and makes the
view-model and service layers testable without a running desktop UI or backend.

### Windows-Specific Behavior

The desktop client currently owns Windows-specific integration such as device
storage and global hot-key behavior. OAuth uses a local HTTP listener for the
desktop redirect and uses PKCE state and code-verifier values to protect the
flow.

The application also runs as a tray-oriented desktop process. It initializes
the stored device identity and refresh-token session at startup, loads initial
clippings, and then starts the SignalR connection when authentication succeeds.

## Mobile and Web Frontend

`scissors-mobile` is a TypeScript Expo project that targets native mobile
platforms and the web through React Native Web. There is currently one shared
frontend codebase rather than separate mobile and web applications.

### Frontend Structure

```text
src/api/          HTTP API models, requests, and SignalR connection
src/components/   Reusable UI components
src/screens/      Screen-level UI
src/context/      Shared application state
src/util/         Storage and platform helpers
```

The application uses React components and hooks. `AppContext` provides shared
authentication and clipping state to the screen tree. The current root flow:

1. Attempts to restore the authentication session.
2. Loads the user's clippings after a successful refresh.
3. Starts a SignalR connection for authenticated users.
4. Refreshes access tokens before expiry and when the app returns to the active
   state.
5. Updates local clipping state from real-time events and resynchronizes over
   HTTP after reconnects.

### Platform Differences

The API layer branches on the Expo platform when selecting refresh-token flows.
Native storage uses Expo Secure Store, while web behavior is isolated behind
the storage and platform utility modules.

The mobile and web clients share API models and most application behavior, but
they may use different OAuth routes and storage implementations. Platform-
specific behavior should stay behind utilities or small API/configuration
boundaries instead of spreading platform checks through screens.

## Testing

Tests are organized with the project they exercise.

### Backend Tests

`Scissors.API.Tests` uses xUnit and includes tests for:

- Clipping handlers
- Authentication and refresh-token handlers
- Request DTO validation
- Clipping response mapping
- EF Core model behavior

The test project references the API project and uses EF Core InMemory support
for isolated persistence tests. `Infrastructure/RecordingHubContext.cs` and
`ApiTestHelpers.cs` provide test doubles for notifications and shared setup.

### Desktop Tests

`Scissors.Desktop.Tests` uses xUnit and tests:

- View-model behavior
- Clipping services and stores
- Authentication sessions
- Token refresh behavior
- OAuth utilities
- Windows-facing abstractions through test doubles

The desktop tests target the same Windows framework as the desktop application.

### Mobile/Web Tests

`scissors-mobile` uses Vitest for TypeScript tests. Current tests cover API
configuration, API helpers, SignalR setup, storage, platform detection, and
theme behavior.

Tests should focus on observable behavior and keep network, OS, and UI-runtime
dependencies behind replaceable boundaries where practical.

## Architectural Boundaries

The primary boundaries across the system are:

- **Client/API boundary:** clients communicate through versioned HTTP endpoints
  and the authenticated SignalR hub.
- **API/persistence boundary:** only the API uses `ScissorsDbContext` and
  database entities.
- **Authentication boundary:** clients store and refresh tokens; the API
  validates access tokens and owns refresh-token persistence.
- **Real-time boundary:** SignalR delivers notifications, while HTTP provides
  initial loading and recovery synchronization.
- **Platform boundary:** Windows behavior lives in desktop services; native and
  web differences live in mobile/web utilities and configuration.
- **Deployment boundary:** the API is containerized and deployed separately
  from the client applications, which are packaged for their respective
  platforms.

These boundaries allow the desktop, mobile, and web applications to be released
independently while sharing a stable backend contract. See
`docs/versioning.md` for the release and tag strategy.
