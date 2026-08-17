using System;

public class AuthSession
{
    public static string? AccessToken { get; private set; } = null;

    public static bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public static void SetToken(string? token)
    {
        Console.WriteLine($"token: {token}");
        AccessToken = token;
    }

    public static void Clear()
    {
        AccessToken = null;
    }
}