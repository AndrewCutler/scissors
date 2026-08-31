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
            return dtos
                .Select(c => Clipping.FromDTO(c))
                .OrderByDescending(c => c.CapturedAt)
                .ToList();
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

            _store.RemoveTemporary((Guid)clipping.TemporaryId!);
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

    public async Task DeleteClippingAsync(Clipping clipping)
    {
        if (clipping.TemporaryId is Guid tempId)
        {
            _store.RemoveTemporary(tempId);
            return;
        }

        if (!_authSession.IsAuthenticated)
        {
            throw new InvalidOperationException("Cannot get clippings; not authenticated.");
        }

        try
        {
            if (clipping.Id is int id)
            {

                await _apiClient.DeleteClippingAsync(_authSession.AccessToken!, id);
                _store.Remove(id);
            }
            else
            {
                throw new InvalidOperationException("Cannot delete clipping with null Id.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to delete clipping.");
            throw;
        }

    }

    public void AddClipping(Clipping clipping)
    {
        _store.Add(clipping);
    }
}
