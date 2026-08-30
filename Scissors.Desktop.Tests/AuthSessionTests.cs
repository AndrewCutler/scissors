using Xunit;

namespace Scissors.Desktop.Tests;

public class AuthSessionTests
{
    [Fact]
    public void SetTokenRaisesPropertyChangedForAuthenticationState()
    {
        var session = new AuthSession();
        var changes = new List<string>();
        session.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        session.SetToken("token");

        Assert.Equal("token", session.AccessToken);
        Assert.True(session.IsAuthenticated);
        Assert.Equal(new[] { nameof(AuthSession.AccessToken), nameof(AuthSession.IsAuthenticated) }, changes);
    }

    [Fact]
    public void SettingTheSameTokenDoesNotRaiseDuplicateNotifications()
    {
        var session = new AuthSession();
        var changes = new List<string>();
        session.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        session.SetToken("token");
        session.SetToken("token");

        Assert.Equal(2, changes.Count);
    }

    [Fact]
    public void ClearRaisesNotificationsAndRemovesTheExpiration()
    {
        var session = new AuthSession();
        var changes = new List<string>();
        session.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        session.SetToken("token");
        session.SetExpiresAt(new DateTime(2026, 8, 30, 14, 0, 0));
        changes.Clear();

        session.Clear();

        Assert.Null(session.AccessToken);
        Assert.Null(session.ExpiresAt);
        Assert.False(session.IsAuthenticated);
        Assert.Equal(new[] { nameof(AuthSession.AccessToken), nameof(AuthSession.IsAuthenticated), nameof(AuthSession.ExpiresAt) }, changes);
    }
}
