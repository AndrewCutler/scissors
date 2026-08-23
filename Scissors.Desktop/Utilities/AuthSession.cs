using System;
using System.ComponentModel;

public class AuthSession : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string? AccessToken { get; private set; } = null;
    public DateTime? ExpiresAt { get; private set; } = null;
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public void SetToken(string? token)
    {
        if (string.Equals(AccessToken, token, StringComparison.Ordinal))
        {
            return;
        }

        AccessToken = token;
        OnPropertyChanged(nameof(AccessToken));
        OnPropertyChanged(nameof(IsAuthenticated));
    }

    public void SetExpiresAt(DateTime? expiresAt)
    {
        if (ExpiresAt == expiresAt)
        {
            return;
        }

        ExpiresAt = expiresAt;
        OnPropertyChanged(nameof(ExpiresAt));
    }

    public void Clear()
    {
        var hadToken = AccessToken is not null;
        var hadExpiry = ExpiresAt is not null;

        AccessToken = null;
        ExpiresAt = null;

        if (hadToken)
        {
            OnPropertyChanged(nameof(AccessToken));
            OnPropertyChanged(nameof(IsAuthenticated));
        }

        if (hadExpiry)
        {
            OnPropertyChanged(nameof(ExpiresAt));
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
