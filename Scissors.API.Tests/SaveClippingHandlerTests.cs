using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Scissors.API.Handlers.Clippings;
using Scissors.API.Models.DTOs;
using Scissors.API.Models.Entities;
using Scissors.API.Tests.Infrastructure;
using Xunit;

namespace Scissors.API.Tests;

public class SaveClippingHandlerTests
{
    [Fact]
    public async Task ReturnsUnauthorizedWhenSubjectCannotBeParsed()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var hub = new RecordingHubContext();

        var result = await SaveClippingHandler.Handle(
            new SaveClippingRequestDTO
            {
                Text = "clip",
                CapturedAt = DateTimeOffset.UtcNow,
            },
            db,
            hub,
            ApiTestHelpers.CreateClaimsPrincipal("not-a-number"),
            CancellationToken.None);

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
        Assert.Empty(db.Clippings);
        Assert.Empty(hub.RecordingClients.Proxy.Calls);
    }

    [Fact]
    public async Task PersistsTheClippingAndBroadcastsItToTheUsersHubConnection()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var hub = new RecordingHubContext();
        var capturedAt = new DateTimeOffset(2026, 8, 30, 13, 0, 0, TimeSpan.Zero);

        var result = await SaveClippingHandler.Handle(
            new SaveClippingRequestDTO
            {
                Text = "new clipping",
                CapturedAt = capturedAt,
            },
            db,
            hub,
            ApiTestHelpers.CreateClaimsPrincipal("42"),
            CancellationToken.None);

        var (statusCode, body, headers) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status201Created, statusCode);
        Assert.Equal("/api/v1/clippings/1", headers.Location.ToString());

        var dto = JsonSerializer.Deserialize<ClippingResponseDTO>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(dto);
        Assert.Equal(1, dto!.Id);
        Assert.Equal("new clipping", dto.Text);
        Assert.Equal(capturedAt, dto.CapturedAt);

        var clipping = await db.Clippings.SingleAsync();
        Assert.Equal(42, clipping.UserId);
        Assert.Equal("new clipping", clipping.Text);
        Assert.Equal(capturedAt, clipping.CapturedAt);

        Assert.Equal("42", hub.RecordingClients.LastUserId);
        var call = Assert.Single(hub.RecordingClients.Proxy.Calls);
        Assert.Equal("NewClipping", call.Method);
        var sentDto = Assert.IsType<ClippingResponseDTO>(call.Args[0]);
        Assert.Equal(dto.Id, sentDto.Id);
        Assert.Equal(dto.Text, sentDto.Text);
    }
}
