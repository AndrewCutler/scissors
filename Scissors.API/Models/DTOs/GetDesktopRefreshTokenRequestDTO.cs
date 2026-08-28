using System.ComponentModel.DataAnnotations;

public record GetDesktopRefreshTokenRequestDTO
{
    [Required, MinLength(1)]
    public string RefreshToken { get; init; } = string.Empty;
}