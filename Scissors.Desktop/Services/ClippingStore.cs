using System;
using System.Collections.Generic;
using System.Linq;

public class ClippingStore : IClippingStore
{
    private List<Clipping>? _clippings { get; set; }

    public void Add(Clipping clipping)
    {
        _clippings?.Add(clipping);
    }

    public void Init(List<Clipping> clippings)
    {
        if (_clippings is not null)
        {
            throw new InvalidOperationException("Cannot initialize clipping store: already initialized.");
        }

        foreach (var clipping in clippings)
        {
            Console.WriteLine($"Clipping {clipping.Id}: {clipping.Text}");
        }

        _clippings = clippings;
    }

    public void Remove(Guid temporaryId)
    {
        var clipping = _clippings?.FirstOrDefault(c => c.TemporaryId == temporaryId);
        if (clipping is not null)
        {
            _clippings?.Remove(clipping);
        }
    }

    public IReadOnlyList<Clipping>? Get()
    {
        return _clippings;
    }
}