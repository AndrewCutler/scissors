using System.ComponentModel.DataAnnotations;

public record CompleteGoogleOAuthWebRequestDTO
{
    [Required]
    [MinLength(1)]
    public string IdToken { get; set; } = string.Empty;
}