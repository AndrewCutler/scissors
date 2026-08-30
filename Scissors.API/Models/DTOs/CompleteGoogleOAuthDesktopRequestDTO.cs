using System.ComponentModel.DataAnnotations;

public record CompleteGoogleOAuthDesktopRequestDTO
{
    [Required]
    [MinLength(1)]
    public string Code { get; init; } = string.Empty;
    [Required]
    [MinLength(1)]
    public string CodeVerifier { get; init; } = string.Empty;
}