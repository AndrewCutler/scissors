using Scissors.API.Models.Entities;

namespace Scissors.API.Models.DTOs;

public sealed record ClippingResponseDTO
{
    public int Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTimeOffset CapturedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public static ClippingResponseDTO FromEntity(Clipping clipping)
    {
        return new ClippingResponseDTO
        {
            Id = clipping.Id,
            Text = clipping.Text,
            CapturedAt = clipping.CapturedAt,
            CreatedAt = clipping.CreatedAt,
        };
    }
}
