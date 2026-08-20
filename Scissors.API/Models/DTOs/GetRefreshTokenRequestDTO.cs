using System.ComponentModel.DataAnnotations;

public record GetRefreshTokenRequestDTO
{
    [Required, MinLength(1)]
    public string RefreshToken { get; init; } = string.Empty;
}