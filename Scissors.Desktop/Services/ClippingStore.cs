using System;
using System.Collections.Generic;
using System.Linq;

public class ClippingStore : IClippingStore
{
    private List<Clipping> _clippings { get; set; } = [];

    public void Add(Clipping clipping)
    {
        _clippings.Add(clipping);
    }

    public void Remove(Guid temporaryId)
    {
        var clipping = _clippings.FirstOrDefault(c => c.TemporaryId == temporaryId);
        if (clipping is not null)
        {
            _clippings.Remove(clipping);
        }
    }

    public IReadOnlyList<Clipping> Get()
    {
        return _clippings;
    }
}