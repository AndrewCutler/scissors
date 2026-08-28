using System.ComponentModel.DataAnnotations;

public record CompleteGoogleOAuthDesktopRequestDTO
{
    [Required]
    [MinLength(1)]
    public string Code { get; init; } = string.Empty;
}