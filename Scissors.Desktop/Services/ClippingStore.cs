using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public class ClippingStore : IClippingStore
{
    private readonly ObservableCollection<Clipping> _clippings = new();
    private readonly ReadOnlyObservableCollection<Clipping> _readOnlyClippings;
    private bool _isInitialized = false;

    public ClippingStore()
    {
        _readOnlyClippings = new ReadOnlyObservableCollection<Clipping>(_clippings);
    }

    public ReadOnlyObservableCollection<Clipping> Clippings => _readOnlyClippings;

    public void Add(Clipping clipping)
    {
        InsertSorted(clipping);
    }

    public void Init(List<Clipping> clippings)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("Cannot initialize clipping store: already initialized.");
        }

        _clippings.Clear();

        foreach (var clipping in clippings.OrderByDescending(c => c.CapturedAt))
        {
            _clippings.Add(clipping);
        }

        _isInitialized = true;
    }

    public void Reset()
    {
        _clippings.Clear();
        _isInitialized = false;
    }

    public void Remove(int id)
    {
        var clipping = _clippings.FirstOrDefault(c => c.Id == id);
        if (clipping is not null)
        {
            _clippings.Remove(clipping);
        }
    }

    public void RemoveTemporary(Guid temporaryId)
    {
        var clipping = _clippings.FirstOrDefault(c => c.TemporaryId == temporaryId);
        if (clipping is not null)
        {
            _clippings.Remove(clipping);
        }
    }

    private void InsertSorted(Clipping clipping)
    {
        var index = 0;
        while (index < _clippings.Count && _clippings[index].CapturedAt > clipping.CapturedAt)
        {
            index++;
        }

        _clippings.Insert(index, clipping);
    }
}
