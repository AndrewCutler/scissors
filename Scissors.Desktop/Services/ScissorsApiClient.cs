using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Scissors.Services;

public sealed class ScissorsApiClient : IScissorsApiClient
{
    private readonly HttpClient _httpClient;

    public ScissorsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GetRefreshTokenResponseDTO?> GetRefreshTokenAsync(string refreshToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("auth/refresh", new
        {
            refreshToken,
        });

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<GetRefreshTokenResponseDTO>();
    }

    public async Task<GoogleAuthResponseDTO?> CompleteGoogleOAuthAsync(string code)
    {
        using var response = await _httpClient.PostAsJsonAsync("auth/google/native", new
        {
            Code = code,
        });

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<GoogleAuthResponseDTO>();
    }


    public async Task<bool> LogOutAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

        using var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    public async Task<List<ClippingResponseDTO>> GetClippingsAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "clippings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var clippings = await response.Content.ReadFromJsonAsync<List<ClippingResponseDTO>>();

        return clippings ?? new();
    }

    public async Task<ClippingResponseDTO> SaveClippingAsync(string accessToken, DateTimeOffset capturedAt, string text)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "clippings")
        {
            Content = JsonContent.Create(new
            {
                capturedAt,
                text,
            }),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var clipping = await response.Content.ReadFromJsonAsync<ClippingResponseDTO>();

        return clipping ?? throw new InvalidOperationException("Failed to save clipping.");
    }

    public async Task DeleteClippingAsync(string accessToken, int id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"clippings/{id}");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();
    }
}
