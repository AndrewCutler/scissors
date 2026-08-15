using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Scissors.API.Configuration;
using Scissors.API.Data;
using Scissors.API.Models;
using Scissors.API.Models.Entities;

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

// builder.Services.AddAuthentication().AddJwtBearer("schema", Options =>
// {

// });

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

v1.MapPost("/clippings", async ([FromBody] SaveClippingRequestDTO request, AppDbContext db, CancellationToken cancellationToken) =>
{
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

v1.MapGet("/auth/google", async () =>
{
    return Results.Redirect("https://google.com");
}).WithName("GoogleOAuth");

app.Run();
