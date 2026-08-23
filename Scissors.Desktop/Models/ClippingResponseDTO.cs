using System;
using System.ComponentModel.DataAnnotations;

public record ClippingResponseDTO
{
    [Required, Range(1, int.MaxValue)]
    public int Id { get; init; }
    [Required, MinLength(1)]
    public string Text { get; init; } = string.Empty;
    [Required]
    public DateTimeOffset CapturedAt { get; set; }
    [Required]
    public DateTimeOffset CreatedAt { get; set; }
}