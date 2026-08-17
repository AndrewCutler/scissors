using System;
using System.Text.Json.Serialization;

public record GoogleAuthResponseDTO
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; init; } = string.Empty;
    [JsonPropertyName("accessTokenExpiresAt")]
    public DateTime AccessTokenExpiresAt { get; init; }
}