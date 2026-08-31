using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Scissors.Services;

public interface IScissorsApiClient
{
    Task<List<ClippingResponseDTO>> GetClippingsAsync(string accessToken);
    
    Task<GetRefreshTokenResponseDTO?> GetRefreshTokenAsync(string refreshToken, string deviceId);

    Task<GoogleAuthResponseDTO?> CompleteGoogleOAuthAsync(string code, string codeVerifier, string deviceId);

    Task<bool> LogOutAsync(string accessToken);

    Task<ClippingResponseDTO> SaveClippingAsync(string accessToken, DateTimeOffset capturedAt, string text);

    Task DeleteClippingAsync(string accessToken, int id);
}
