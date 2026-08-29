using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Scissors.API.Data;
using Scissors.API.Models.Entities;

namespace Scissors.API.Handlers.Clippings;

public static class SaveClippingHandler
{
    public static async Task<IResult> Handle(
        [FromBody] SaveClippingRequestDTO request,
        ScissorsDbContext db,
        ClaimsPrincipal claims,
        CancellationToken cancellationToken)
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
    }
}
