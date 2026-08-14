namespace Scissors.API.Models.Entities;

public sealed class Clipping
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset CapturedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
