using System;
using System.Threading.Tasks;

namespace Scissors.Services;

public interface IScissorsApiClient
{
    Task<GetRefreshTokenResponseDTO?> GetRefreshTokenAsync(string refreshToken);

    Task<GoogleAuthResponseDTO?> CompleteGoogleOAuthAsync(string code);

    Task<bool> LogOutAsync(string accessToken);

    Task<bool> SendClippingAsync(string accessToken, DateTimeOffset capturedAt, string text);
}
