using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scissors.API.Data;
using Scissors.API.Models.DTOs;

namespace Scissors.API.Handlers.Clippings;

public static class GetClippingsHandler
{
    public static async Task<IResult> Handle(
        ScissorsDbContext db,
        ClaimsPrincipal claims,
        CancellationToken cancellationToken,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 30)
    {
        var userIdClaim = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var clippings = await db.Clippings
            .Where(c => c.UserId == userId)
            .Where(c => c.DeletedAt == null)
            .AsNoTracking()
            .OrderByDescending(clipping => clipping.CapturedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Results.Ok(clippings.Select(ClippingResponseDTO.FromEntity));
    }
}
