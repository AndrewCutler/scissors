using Scissors.API.Models.DTOs;
using Scissors.API.Models.Entities;
using Xunit;

namespace Scissors.API.Tests;

public class ClippingResponseDtoTests
{
    [Fact]
    public void FromEntityCopiesTheExpectedFields()
    {
        var entity = new Clipping
        {
            Id = 12,
            Text = "hello world",
            CapturedAt = new DateTimeOffset(2026, 8, 30, 12, 15, 0, TimeSpan.Zero),
            CreatedAt = new DateTimeOffset(2026, 8, 30, 12, 16, 0, TimeSpan.Zero),
        };

        var dto = ClippingResponseDTO.FromEntity(entity);

        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.Text, dto.Text);
        Assert.Equal(entity.CapturedAt, dto.CapturedAt);
        Assert.Equal(entity.CreatedAt, dto.CreatedAt);
    }
}
