using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Scissors.API.Data;
using Scissors.API.Hub;
using Scissors.API.Models.DTOs;

namespace Scissors.API.Handlers.Clippings;

public static class UpdateClippingHandler
{
    public static async Task<IResult> Handle(
        [FromRoute] int id,
        [FromBody] SaveClippingRequestDTO request,
        ScissorsDbContext db,
        IHubContext<ClippingsHub> hub,
        ClaimsPrincipal claims,
        CancellationToken cancellationToken)
    {
        var userIdClaim = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var clipping = await db.Clippings.FirstOrDefaultAsync(
            c => c.Id == id,
            cancellationToken);

        if (clipping is null)
        {
            return Results.NotFound();
        }

        if (clipping.UserId != userId)
        {
            return Results.Unauthorized();
        }

        if (clipping.DeletedAt is not null)
        {
            return Results.NotFound();
        }

        clipping.Text = request.Text;
        clipping.CapturedAt = request.CapturedAt;

        await db.SaveChangesAsync(cancellationToken);

        var response = ClippingResponseDTO.FromEntity(clipping);

        await hub.Clients
            .User(userId.ToString())
            .SendAsync("UpdatedClipping", response, cancellationToken);

        return Results.Ok(response);
    }
}
