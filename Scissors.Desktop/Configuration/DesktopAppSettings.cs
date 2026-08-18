using System;
using Microsoft.Extensions.Configuration;

namespace Scissors.Configuration;

public sealed record DesktopAppSettings
{
    public required string ApiUrl { get; init; }

    public required OAuthSettings OAuth { get; init; }

}

public sealed record OAuthSettings
{
    public GoogleOAuthSettings Google { get; init; } = default!;
}

public sealed record GoogleOAuthSettings
{
    public required string ClientId { get; init; }

    public required string RedirectUri { get; init; }
}
