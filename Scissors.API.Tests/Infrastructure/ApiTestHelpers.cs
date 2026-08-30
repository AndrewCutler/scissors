using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Scissors.API.Data;

namespace Scissors.API.Tests.Infrastructure;

internal static class ApiTestHelpers
{
    public static ClaimsPrincipal CreateClaimsPrincipal(string? subject = null)
    {
        var claims = new List<Claim>();

        if (subject is not null)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, subject));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    public static ScissorsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ScissorsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ScissorsDbContext(options);
    }

    public static async Task<(int StatusCode, string Body, IHeaderDictionary Headers)> ExecuteAsync(IResult result, HttpContext? context = null)
    {
        context ??= new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            Response =
            {
                Body = new MemoryStream()
            }
        };

        context.RequestServices ??= new ServiceCollection().AddLogging().BuildServiceProvider();

        if (context.Response.Body is null || context.Response.Body == Stream.Null)
        {
            context.Response.Body = new MemoryStream();
        }

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        return (context.Response.StatusCode, body, context.Response.Headers);
    }
}
