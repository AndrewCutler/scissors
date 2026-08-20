using System;

public class AuthSession
{
    public string? AccessToken { get; private set; } = null;
    public DateTime? ExpiresAt { get; private set; } = null;
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public void SetToken(string? token)
    {
        AccessToken = token;
    }

    public void SetExpiresAt(DateTime? expiresAt)
    {
        ExpiresAt = expiresAt;
    }

    public void Clear()
    {
        AccessToken = null;
        ExpiresAt = null;
    }
}