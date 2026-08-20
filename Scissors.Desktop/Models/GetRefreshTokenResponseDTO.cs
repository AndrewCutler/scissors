using System;
using System.Text.Json.Serialization;

public class GetRefreshTokenResponseDTO
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; init; } = string.Empty;
    [JsonPropertyName("accessTokenExpiresAt")]
    public DateTime AccessTokenExpiresAt { get; init; }
}