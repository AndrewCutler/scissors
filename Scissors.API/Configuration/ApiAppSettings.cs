using Microsoft.Extensions.Configuration;

namespace Scissors.API.Configuration;

public sealed record ApiAppSettings
{
    public required ConnectionStrings ConnectionStrings { get; init; }
    public required OAuth OAuth { get; init; }
    public required Jwt Jwt { get; init; }
}

public sealed record ConnectionStrings
{
    public required string Postgres { get; init; } = string.Empty;
}

public sealed record Jwt
{
    public required string Issuer { get; set; } = string.Empty;
    public required string Audience { get; set; } = string.Empty;
    public required string Secret { get; set; } = string.Empty;
}

public sealed record OAuth
{
    public required GoogleOAuthSettings Google { get; init; } = default!;
}

public sealed record GoogleOAuthSettings
{
    public required Desktop Desktop { get; set; } = default!;
    public required Web Web { get; set; } = default!;
}

public sealed record Desktop
{
    public required string ClientId { get; set; } = string.Empty;
    public required string ClientSecret { get; set; } = string.Empty;
    public required string RedirectUri { get; set; } = string.Empty;
}

public sealed record Web
{
    public required string ClientId { get; set; } = string.Empty;
    public required string ClientSecret { get; set; } = string.Empty;
}