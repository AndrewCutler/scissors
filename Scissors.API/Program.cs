using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Scissors.API.Configuration;
using Scissors.API.Data;
using System.Text;
using Scissors.API.Models.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using Serilog;

var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "scissors-api-.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        logPath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    var appSettings = builder.Configuration.Get<ApiAppSettings>()!;

    builder.Services.AddDbContext<ScissorsDbContext>(options =>
    {
        options.UseNpgsql(appSettings.ConnectionStrings.Postgres);
    });
    builder.Services.AddSingleton(appSettings);

    builder.Services.AddSingleton<
        IConfigurationManager<OpenIdConnectConfiguration>>(
        _ => new ConfigurationManager<OpenIdConnectConfiguration>(
            "https://accounts.google.com/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever()));

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddHealthChecks();

    builder.Services.AddHttpClient();

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });

    builder.Services.AddCors((options) =>
    {
        string[] methods = ["GET", "POST"];

        options.AddPolicy("Desktop", policy =>
        {
            policy.WithOrigins("").WithMethods(methods);
        });

        options.AddPolicy("Mobile", policy =>
        {
            policy.WithOrigins("").WithMethods(methods);
        });

        options.AddPolicy("ChromeExtension", policy =>
        {
            policy.WithOrigins("").WithMethods(methods);
        });
    });

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = appSettings.Jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = appSettings.Jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(appSettings.Jwt.Secret)),

            ValidateLifetime = true,
        };

        options.MapInboundClaims = false;
    });

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Scissors.API v1");
        });
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    var api = app.NewVersionedApi();
    var v1 = api.MapGroup("/api/v1")
        .HasApiVersion(1.0);

    app.MapHealthChecks("/health").AllowAnonymous();

    v1.MapGet("/clippings", async (ScissorsDbContext db, ClaimsPrincipal claims, CancellationToken cancellationToken) =>
    {
        var userIdClaim = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var clippings = await db.Clippings
            .Where(c => c.UserId == userId)
            .AsNoTracking()
            .OrderByDescending(clipping => clipping.CapturedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(clippings);
    })
    .WithName("GetClippings");

    v1.MapPost("/clippings", async ([FromBody] SaveClippingRequestDTO request, ScissorsDbContext db, ClaimsPrincipal claims, CancellationToken cancellationToken) =>
    {
        var userIdClaim = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var clipping = new Clipping
        {
            Text = request.Text,
            UserId = userId,
            CapturedAt = request.CapturedAt,
        };

        db.Clippings.Add(clipping);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/clippings/{clipping.Id}", clipping);
    })
    .WithName("SaveClipping");

    // TODO: in the future, handle multiple clients.
    v1.MapPost("/auth/google", async (
        [FromBody] CompleteGoogleOAuthRequestDTO dto,
        ScissorsDbContext db,
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
        IHttpClientFactory httpClientFactory) =>
    {
        var httpClient = httpClientFactory.CreateClient();

        var response = await httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = dto.Code,
                ["client_id"] = appSettings.OAuth.Google.Desktop.ClientId,
                ["client_secret"] = appSettings.OAuth.Google.Desktop.ClientSecret,
                ["redirect_uri"] = appSettings.OAuth.Google.Desktop.RedirectUri,
                ["grant_type"] = "authorization_code",
                // ["code_verifier"] = dto.CodeVerifier
            }));

        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponseDTO>();

        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false,
        };
        var configuration = await configurationManager.GetConfigurationAsync(CancellationToken.None);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://accounts.google.com",
            ValidateAudience = true,
            ValidAudience = appSettings.OAuth.Google.Desktop.ClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var principal = handler.ValidateToken(
            tokenResponse?.IdToken,
            validationParameters,
            out var validatedToken);

        var googleUserId = principal.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Google user ID missing");

        var externalIdentity = await db.ExternalIdentities
            .SingleOrDefaultAsync(e => e.Subject == googleUserId && e.Provider == ExternalIdentityProvider.Google);

        int userId;
        if (externalIdentity is null)
        {
            var user = new User
            {
                ExternalIdentities = [new ExternalIdentity {
                    Provider = ExternalIdentityProvider.Google,
                    Subject = googleUserId,
                }]
            };
            db.Add(user);
            await db.SaveChangesAsync();

            userId = user.Id;
        }
        else
        {
            userId = externalIdentity.UserId;
        }

        var claims = new[]
        {
            // TODO: custom userId claim
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
        };
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(appSettings.Jwt.Secret));
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: appSettings.Jwt.Issuer,
            audience: appSettings.Jwt.Audience,
            claims: claims,
            expires: expiresAt.DateTime,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshTokenBytes = RandomNumberGenerator.GetBytes(64);
        var refreshToken = Convert.ToBase64String(refreshTokenBytes);

        var refreshTokenHash = Convert.ToBase64String(SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)));

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = refreshTokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });

        await db.SaveChangesAsync();

        return Results.Ok(new AuthenticationResponseDTO
        {
            AccessToken = jwt,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = expiresAt,
        });
    }).AllowAnonymous().WithName("CompleteGoogleOAuth");

    v1.MapPost("/auth/refresh", async ([FromBody] GetRefreshTokenRequestDTO request, ScissorsDbContext db) =>
    {
        var refreshTokenHash = Convert.ToBase64String(SHA256.HashData(
                Encoding.UTF8.GetBytes(request.RefreshToken)));

        var tokenFromStorage = await db.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash);

        if (tokenFromStorage is null || tokenFromStorage.RevokedAt is not null || tokenFromStorage.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Results.Unauthorized();
        }

        tokenFromStorage.RevokedAt = DateTimeOffset.UtcNow;

        // TODO: move to method and update auth/google route too
        var claims = new[]
        {
            // TODO: custom userId claim
            new Claim(JwtRegisteredClaimNames.Sub, tokenFromStorage.UserId.ToString())
        };
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(appSettings.Jwt.Secret));
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: appSettings.Jwt.Issuer,
            audience: appSettings.Jwt.Audience,
            claims: claims,
            expires: expiresAt.DateTime,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshTokenBytes = RandomNumberGenerator.GetBytes(64);
        var newRefreshToken = Convert.ToBase64String(refreshTokenBytes);

        var newRefreshTokenHash = Convert.ToBase64String(SHA256.HashData(
                Encoding.UTF8.GetBytes(newRefreshToken)));

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = tokenFromStorage.UserId,
            TokenHash = newRefreshTokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });

        await db.SaveChangesAsync();

        return Results.Ok(new GetRefreshTokenResponseDTO
        {
            AccessToken = jwt,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = expiresAt,
        });
    }).AllowAnonymous().WithName("RefreshToken");


    Log.Information("Starting Scissors API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Scissors API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
