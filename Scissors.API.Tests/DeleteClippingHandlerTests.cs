using Microsoft.EntityFrameworkCore;
using Scissors.API.Handlers.Clippings;
using Scissors.API.Models.Entities;
using Scissors.API.Tests.Infrastructure;
using Xunit;

namespace Scissors.API.Tests;

public class DeleteClippingHandlerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    public async Task ReturnsUnauthorizedWhenSubjectCannotBeParsed(string? subject)
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var hub = new RecordingHubContext();

        var result = await DeleteClippingHandler.Handle(
            ApiTestHelpers.CreateClaimsPrincipal(subject),
            1,
            db,
            hub,
            CancellationToken.None);

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
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
            Text = "other user's clipping",
            CapturedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        });
        await db.SaveChangesAsync();

        var result = await DeleteClippingHandler.Handle(
            ApiTestHelpers.CreateClaimsPrincipal("7"),
            1,
            db,
            hub,
            CancellationToken.None);

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
    }

    [Fact]
    public async Task SoftDeletesTheClippingAndBroadcastsTheDeletion()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var hub = new RecordingHubContext();
        db.Clippings.Add(new Clipping
        {
            Id = 1,
            UserId = 7,
            Text = "remove me",
            CapturedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        });
        await db.SaveChangesAsync();

        var result = await DeleteClippingHandler.Handle(
            ApiTestHelpers.CreateClaimsPrincipal("7"),
            1,
            db,
            hub,
            CancellationToken.None);

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, statusCode);

        var clipping = await db.Clippings.SingleAsync();
        Assert.NotNull(clipping.DeletedAt);

        var call = Assert.Single(hub.RecordingClients.Proxy.Calls);
        Assert.Equal("DeletedClipping", call.Method);
        Assert.Equal(1, Assert.IsType<int>(call.Args[0]));
    }
}
