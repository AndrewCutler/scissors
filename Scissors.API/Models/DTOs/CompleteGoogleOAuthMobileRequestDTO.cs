using System.ComponentModel.DataAnnotations;

public record CompleteGoogleOAuthMobileRequestDTO
{
    [Required]
    [MinLength(1)]
    public string IdToken { get; set; } = string.Empty;
    [Required]
    [MinLength(1)]
    public string DeviceId { get; set; } = string.Empty;
    [Required]
    public Platform Platform { get; set; }
}