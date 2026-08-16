using System.Text.Json.Serialization;

public record GoogleTokenResponseDTO
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
    [JsonPropertyName("id_token")]
    public string IdToken { get; init; } = string.Empty;
    [JsonPropertyName("scope")]
    public string Scope { get; init; } = string.Empty;
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = string.Empty;
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; } = string.Empty;
}