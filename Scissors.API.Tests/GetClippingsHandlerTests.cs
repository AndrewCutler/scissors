using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Scissors.API.Handlers.Clippings;
using Scissors.API.Models.Entities;
using Scissors.API.Tests.Infrastructure;
using Xunit;

namespace Scissors.API.Tests;

public class GetClippingsHandlerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    public async Task ReturnsUnauthorizedWhenTheSubjectClaimCannotBeParsed(string? subject)
    {
        using var db = ApiTestHelpers.CreateDbContext();

        var result = await GetClippingsHandler.Handle(
            db,
            ApiTestHelpers.CreateClaimsPrincipal(subject),
            CancellationToken.None);

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
    }

    [Fact]
    public async Task ReturnsTheCurrentUsersVisibleClippingsInDescendingOrder()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var now = DateTimeOffset.UtcNow;

        db.Clippings.AddRange(
            new Clipping { Id = 1, UserId = 7, Text = "old", CapturedAt = now.AddMinutes(-20), CreatedAt = now.AddMinutes(-20) },
            new Clipping { Id = 2, UserId = 7, Text = "middle", CapturedAt = now.AddMinutes(-10), CreatedAt = now.AddMinutes(-10) },
            new Clipping { Id = 3, UserId = 7, Text = "latest", CapturedAt = now.AddMinutes(-1), CreatedAt = now.AddMinutes(-1), DeletedAt = now.AddMinutes(-1) },
            new Clipping { Id = 4, UserId = 9, Text = "other user", CapturedAt = now.AddMinutes(-5), CreatedAt = now.AddMinutes(-5) });
        await db.SaveChangesAsync();

        var result = await GetClippingsHandler.Handle(
            db,
            ApiTestHelpers.CreateClaimsPrincipal("7"),
            CancellationToken.None,
            skip: 0,
            take: 2);

        var (statusCode, body, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, statusCode);

        var dtos = JsonSerializer.Deserialize<ClippingResponseDTO[]>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(dtos);
        Assert.Equal(new[] { "middle", "old" }, dtos!.Select(dto => dto.Text).ToArray());
    }

    [Fact]
    public async Task SupportsPaginationOnTheVisibleClippings()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var now = DateTimeOffset.UtcNow;

        db.Clippings.AddRange(
            new Clipping { Id = 1, UserId = 7, Text = "first", CapturedAt = now.AddMinutes(-30), CreatedAt = now.AddMinutes(-30) },
            new Clipping { Id = 2, UserId = 7, Text = "second", CapturedAt = now.AddMinutes(-20), CreatedAt = now.AddMinutes(-20) },
            new Clipping { Id = 3, UserId = 7, Text = "third", CapturedAt = now.AddMinutes(-10), CreatedAt = now.AddMinutes(-10) });
        await db.SaveChangesAsync();

        var result = await GetClippingsHandler.Handle(
            db,
            ApiTestHelpers.CreateClaimsPrincipal("7"),
            CancellationToken.None,
            skip: 1,
            take: 1);

        var (statusCode, body, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, statusCode);

        var dtos = JsonSerializer.Deserialize<ClippingResponseDTO[]>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(dtos);
        Assert.Single(dtos!);
        Assert.Equal("second", dtos[0].Text);
    }
}
