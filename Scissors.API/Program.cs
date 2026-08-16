using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Scissors.API.Configuration;
using Scissors.API.Data;
using Scissors.API.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Scissors.API.Models.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var appSettings = ApiAppSettings.FromConfiguration(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(appSettings.PostgresConnectionString);
});
builder.Services.AddSingleton(appSettings);

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

        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Scissors.API v1");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

var api = app.NewVersionedApi();
var v1 = api.MapGroup("/api/v1")
    .HasApiVersion(1.0);

v1.MapGet("/clippings", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    return await db.Clippings
        .AsNoTracking()
        .OrderByDescending(clipping => clipping.CapturedAt)
        .ToListAsync(cancellationToken);
})
.WithName("GetClippings");

v1.MapPost("/clippings", async ([FromBody] SaveClippingRequestDTO request, AppDbContext db, ClaimsPrincipal claims, CancellationToken cancellationToken) =>
{
    var userIdClaim = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

    if (!int.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var clipping = new Clipping
    {
        Text = request.Text,
        CapturedAt = request.CapturedAt,
    };

    db.Clippings.Add(clipping);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/v1/clippings/{clipping.Id}", clipping);
})
.WithName("SaveClipping");

v1.MapPost("/auth/google", async ([FromBody] CompleteGoogleOAuthRequestDTO dto, AppDbContext db, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    Console.WriteLine(dto.Code);

    var response = await httpClient.PostAsync(
        "https://oauth2.googleapis.com/token",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = dto.Code,
            ["client_id"] = appSettings.GoogleOAuth.ClientId,
            ["client_secret"] = appSettings.GoogleOAuth.ClientSecret,
            ["redirect_uri"] = appSettings.GoogleOAuth.RedirectUri,
            ["grant_type"] = "authorization_code",
            // ["code_verifier"] = dto.CodeVerifier
        }));

    response.EnsureSuccessStatusCode();

    var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponseDTO>();

    var handler = new JwtSecurityTokenHandler
    {
        MapInboundClaims = false,
    };
    var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            "https://accounts.google.com/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());
    var configuration = await configurationManager.GetConfigurationAsync(
        CancellationToken.None);

    var validationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "https://accounts.google.com",
        ValidateAudience = true,
        ValidAudience = appSettings.GoogleOAuth.ClientId,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKeys = configuration.SigningKeys,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    var principal = handler.ValidateToken(
        tokenResponse?.IdToken,
        validationParameters,
        out var validatedToken);

    // foreach (var claim in principal.Claims)
    // {
    //     Console.WriteLine($"{claim.Type} = {claim.Value}");
    // }

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
    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(appSettings.Jwt.Secret));
    var credentials = new SigningCredentials(
        key,
        SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: appSettings.Jwt.Issuer,
        audience: appSettings.Jwt.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials);

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new
    {
        accessToken = jwt,
    });
}).AllowAnonymous().WithName("CompleteGoogleOAuth");

app.Run();
