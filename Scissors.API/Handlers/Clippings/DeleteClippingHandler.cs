using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scissors.API.Data;

namespace Scissors.API.Handlers.Clippings;

public static class DeleteClippingHandler
{
    public static async Task<IResult> Handle(
        ClaimsPrincipal claims,
        [FromRoute] int id,
        ScissorsDbContext db,
        CancellationToken cancellationToken)
    {
        var userIdClaim = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var clipping = await db.Clippings.FirstOrDefaultAsync(c => c.Id == id);
        if (clipping is null)
        {
            return Results.NotFound();
        }

        if (clipping.UserId != userId)
        {
            return Results.Unauthorized();
        }

        clipping.DeletedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok();
    }
}
