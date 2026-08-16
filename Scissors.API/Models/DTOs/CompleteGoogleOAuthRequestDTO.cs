using System.ComponentModel.DataAnnotations;

public record CompleteGoogleOAuthRequestDTO
{
    [Required]
    [MinLength(1)]
    public string Code { get; init; } = string.Empty;
}