using System.ComponentModel.DataAnnotations;

public record GetNativeRefreshTokenRequestDTO
{
    [Required, MinLength(1)]
    public string RefreshToken { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string DeviceId { get; init; } = string.Empty;
}
