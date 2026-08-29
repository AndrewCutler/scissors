using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Scissors.API.Configuration;
using Scissors.API.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using Scissors.API.Handlers.Auth;
using Scissors.API.Handlers.Clippings;

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
        string[] methods = ["GET", "POST", "DELETE"];

        options.AddPolicy("Desktop", policy =>
        {
            policy.WithOrigins("").WithMethods(methods);
        });

        options.AddPolicy("Mobile", policy =>
        {
            policy.WithOrigins("").WithMethods(methods);
        });

        options.AddPolicy("Web", policy =>
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

    v1.MapGet("/clippings", GetClippingsHandler.Handle)
        .WithName("GetClippings");

    v1.MapDelete("/clippings/{id}", DeleteClippingHandler.Handle)
        .WithName("DeleteClipping");

    v1.MapPost("/clippings", SaveClippingHandler.Handle)
        .WithName("SaveClipping");

    v1.MapPost("/auth/google/desktop", CompleteGoogleDesktopOAuthHandler.Handle)
        .AllowAnonymous()
        .WithName("CompleteGoogleDesktopOAuth");

    v1.MapPost("/auth/google/web", CompleteGoogleWebOAuthHandler.Handle)
        .AllowAnonymous()
        .WithName("CompleteGoogleWebOAuth");

    v1.MapPost("/auth/refresh/native", NativeRefreshTokenHandler.Handle)
        .AllowAnonymous()
        .WithName("NativeRefreshToken");

    v1.MapPost("/auth/refresh/web", WebRefreshTokenHandler.Handle)
        .AllowAnonymous()
        .WithName("WebRefreshToken");

    v1.MapPost("/auth/logout", LogoutHandler.Handle);

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
