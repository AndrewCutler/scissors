using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scissors.Services;
using Scissors.ViewModels;

public class ClippingService : IClippingService
{
    private readonly AuthSession _authSession;
    private readonly IClippingStore _store;
    private readonly IScissorsApiClient _apiClient;
    private readonly ILogger<IClippingService> _logger;

    public ClippingService(
        AuthSession authSession,
        IClippingStore store,
        IScissorsApiClient apiClient,
        ILogger<IClippingService> logger)
    {
        _authSession = authSession;
        _store = store;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<List<Clipping>> GetClippingsAsync()
    {
        if (!_authSession.IsAuthenticated)
        {
            throw new InvalidOperationException("Cannot get clippings; not authenticated.");
        }

        var accessToken = _authSession.AccessToken!;

        try
        {

            var dtos = await _apiClient.GetClippingsAsync(accessToken);

            return dtos.Select(c => Clipping.FromDTO(c)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to get clippings.");
            throw;
        }
    }

    public async Task<Clipping> SaveClippingAsync(Clipping clipping)
    {
        if (!_authSession.IsAuthenticated)
        {
            throw new InvalidOperationException("Cannot get clippings; not authenticated.");
        }

        try
        {
            var response = await _apiClient.SaveClippingAsync(_authSession.AccessToken!, clipping.CapturedAt, clipping.Text);

            _store.Remove((Guid)clipping.TemporaryId!);
            var savedClipping = Clipping.FromDTO(response);
            _store.Add(savedClipping);

            return savedClipping;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to get clippings.");
            throw;
        }
    }

    public void AddClipping(Clipping clipping)
    {
        _store.Add(clipping);
    }
}