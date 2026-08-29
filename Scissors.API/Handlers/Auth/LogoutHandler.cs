using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Scissors.API.Data;
using Serilog;

namespace Scissors.API.Handlers.Auth;

public static class LogoutHandler
{
    public static async Task<IResult> Handle(ClaimsPrincipal claims, ScissorsDbContext db)
    {
        var userIdClaim = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            Log.Warning("Attempted logout for unauthenticated user.");
            return Results.Unauthorized();
        }

        var refreshToken = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.ExpiresAt > DateTimeOffset.UtcNow);

        if (refreshToken is null)
        {
            Log.Warning("Attempted logout failed: refresh token not found for userId {userId}", userId);
            return Results.Unauthorized();
        }

        if (refreshToken.RevokedAt is not null)
        {
            Log.Information("Attempted logout for already revoked refresh token for userId {userId}", userId);
        }

        refreshToken.RevokedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok();
    }
}
