using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Scissors.API.Handlers.Clippings;
using Scissors.API.Models.DTOs;
using Scissors.API.Models.Entities;
using Scissors.API.Tests.Infrastructure;
using Xunit;

namespace Scissors.API.Tests;

public class UpdateClippingHandlerTests
{
    [Fact]
    public async Task ReturnsUnauthorizedWhenSubjectCannotBeParsed()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var hub = new RecordingHubContext();

        var result = await UpdateClippingHandler.Handle(
            1,
            new SaveClippingRequestDTO
            {
                Text = "updated",
                CapturedAt = DateTimeOffset.UtcNow,
            },
            db,
            hub,
            ApiTestHelpers.CreateClaimsPrincipal("abc"),
            CancellationToken.None);

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
    }

    [Fact]
    public async Task ReturnsNotFoundWhenTheClippingDoesNotExist()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var hub = new RecordingHubContext();

        var result = await UpdateClippingHandler.Handle(
            99,
            new SaveClippingRequestDTO
            {
                Text = "updated",
                CapturedAt = DateTimeOffset.UtcNow,
            },
            db,
            hub,
            ApiTestHelpers.CreateClaimsPrincipal("7"),
            CancellationToken.None);

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
    }

    [Fact]
    public async Task ReturnsUnauthorizedWhenTheClippingBelongsToAnotherUser()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var hub = new RecordingHubContext();
        db.Clippings.Add(new Clipping
        {
            Id = 1,
            UserId = 9,
            Text = "owned by someone else",
            CapturedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        });
        await db.SaveChangesAsync();

        var result = await UpdateClippingHandler.Handle(
            1,
            new SaveClippingRequestDTO
            {
                Text = "updated",
                CapturedAt = DateTimeOffset.UtcNow,
            },
            db,
            hub,
            ApiTestHelpers.CreateClaimsPrincipal("7"),
            CancellationToken.None);

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
    }

    [Fact]
    public async Task ReturnsNotFoundWhenTheClippingHasBeenSoftDeleted()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var hub = new RecordingHubContext();
        db.Clippings.Add(new Clipping
        {
            Id = 1,
            UserId = 7,
            Text = "deleted",
            CapturedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            DeletedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await UpdateClippingHandler.Handle(
            1,
            new SaveClippingRequestDTO
            {
                Text = "updated",
                CapturedAt = DateTimeOffset.UtcNow,
            },
            db,
            hub,
            ApiTestHelpers.CreateClaimsPrincipal("7"),
            CancellationToken.None);

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
    }

    [Fact]
    public async Task UpdatesTheClippingAndBroadcastsTheChange()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var hub = new RecordingHubContext();
        var capturedAt = new DateTimeOffset(2026, 8, 30, 13, 15, 0, TimeSpan.Zero);
        db.Clippings.Add(new Clipping
        {
            Id = 1,
            UserId = 7,
            Text = "before",
            CapturedAt = capturedAt.AddHours(-1),
            CreatedAt = capturedAt.AddHours(-1),
        });
        await db.SaveChangesAsync();

        var result = await UpdateClippingHandler.Handle(
            1,
            new SaveClippingRequestDTO
            {
                Text = "after",
                CapturedAt = capturedAt,
            },
            db,
            hub,
            ApiTestHelpers.CreateClaimsPrincipal("7"),
            CancellationToken.None);

        var (statusCode, body, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, statusCode);

        var dto = JsonSerializer.Deserialize<ClippingResponseDTO>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(dto);
        Assert.Equal("after", dto!.Text);
        Assert.Equal(capturedAt, dto.CapturedAt);

        var clipping = await db.Clippings.SingleAsync();
        Assert.Equal("after", clipping.Text);
        Assert.Equal(capturedAt, clipping.CapturedAt);

        var call = Assert.Single(hub.RecordingClients.Proxy.Calls);
        Assert.Equal("UpdatedClipping", call.Method);
        Assert.IsType<ClippingResponseDTO>(call.Args[0]);
    }
}
