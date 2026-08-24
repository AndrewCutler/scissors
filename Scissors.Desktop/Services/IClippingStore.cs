using System;
using System.Collections.Generic;

public interface IClippingStore
{
    IReadOnlyList<Clipping>? Get();
    void Init(List<Clipping> clippings);
    void Add(Clipping clipping);
    void Remove(Guid temporaryId);
}