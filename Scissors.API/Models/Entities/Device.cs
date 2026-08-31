public class Device
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public string? PushToken { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = default!;
}

public enum Platform
{
    Web = 1,
    iOS,
    Android,
    Desktop,
}
