# Versioning

Scissors uses independent semantic versions for each deployable component:

- Backend API
- Mobile app
- Desktop app
- Web app

A change to one component does not require a new version or release of the other
components. The repository may contain all components, but each component owns
its own release history.

## Version Tags

Use a component prefix in Git tags so tags are unambiguous in the monorepo:

```text
api-v1.4.0
mobile-v2.1.0
desktop-v1.3.0
web-v0.9.0
```

Tags use [Semantic Versioning](https://semver.org/):

- `MAJOR` for incompatible changes
- `MINOR` for backward-compatible features
- `PATCH` for backward-compatible fixes

The version belongs to the component being released. For example, a mobile
release can be `mobile-v2.2.0` while the desktop app remains at
`desktop-v1.3.0`.

## Releasing Multiple Components

If mobile and desktop are both ready from the same commit, create two tags on
that commit:

```bash
git checkout master
git pull

git tag mobile-v2.1.0
git tag desktop-v1.3.0

git push origin mobile-v2.1.0
git push origin desktop-v1.3.0
```

The tags point to the same source snapshot, but they represent separate
component releases. Each component's GitHub Actions workflow should trigger
only for its own tag prefix.

## GitHub Actions Triggers

For example, the desktop workflow should use:

```yaml
on:
  push:
    tags:
      - "desktop-v*"
```

The mobile, API, and web workflows use `mobile-v*`, `api-v*`, and `web-v*`
respectively.

Use branch-based workflows for continuous deployment when appropriate, and use
component tags for production releases that need an explicit version. Do not
use a single repository-wide version to decide whether every component must be
released.

## Build Versions

Release workflows should derive the component version from the tag instead of
requiring unrelated project files to be edited for every release. For example,
the `desktop-v1.3.0` tag provides the version `1.3.0`:

```yaml
- name: Read component version
  shell: bash
  run: |
    VERSION="${GITHUB_REF_NAME#desktop-v}"
    echo "APP_VERSION=$VERSION" >> "$GITHUB_ENV"
```

That value can be passed to the desktop build and installer:

```yaml
dotnet publish \
  Scissors.Desktop/Scissors.Desktop.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:Version="$APP_VERSION"
```

The same principle applies to the mobile and web build systems.

## Backend and Client Compatibility

The deployed backend build version and the public API contract version are
separate concepts. A backend deployment might be identified by a commit SHA or
an API release such as `api-v1.4.0`, while the API contract remains `v1`.

Prefer backward-compatible API changes:

- Add fields and endpoints without breaking existing clients.
- Deprecate fields before removing them.
- Make breaking API changes a new major contract version.
- Record minimum supported mobile and desktop versions when necessary.

Before releasing a client, verify that its required backend API contract is
available in production. A client release does not automatically imply that
the other clients need to be rebuilt.

## Release Checklist

1. Merge the component changes into `master`.
2. Confirm the commit is tested against the required backend version.
3. Create a tag using the component prefix and semantic version.
4. Push the tag to GitHub.
5. Let the matching GitHub Actions workflow build and publish that component.
6. Create additional component tags only when additional components are ready.

