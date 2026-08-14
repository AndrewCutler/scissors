using System.ComponentModel.DataAnnotations;

public record SaveClippingRequestDTO
{
    [StringLength(maximumLength: 2000, MinimumLength = 1, ErrorMessage = "Text must be between 1 and 2000 characters.")]
    public string Text { get; init; } = string.Empty;

    public DateTimeOffset CapturedAt { get; init; }
}