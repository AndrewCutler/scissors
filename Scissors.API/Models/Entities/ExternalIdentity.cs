public class ExternalIdentity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public ExternalIdentityProvider Provider { get; set; }

    public User User { get; set; } = default!;
}

public enum ExternalIdentityProvider
{
    Google = 1,
}