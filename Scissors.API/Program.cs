using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
}

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Scissors.API v1");
});


app.UseHttpsRedirection();

app.MapGet("/clippings", () =>
{
    return;
})
.WithName("GetClippings");

app.MapPost("/clippings", ([FromBody] SaveClippingRequestDTO request) =>
{
    Console.WriteLine("POST");
    Console.WriteLine(request.Text);
    return;
})
.WithName("PostClipping");

app.Run();
