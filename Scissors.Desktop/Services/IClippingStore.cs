using System;
using System.Collections.Generic;

public interface IClippingStore
{
    IReadOnlyList<Clipping> Get();
    void Add(Clipping clipping);
    void Remove(Guid temporaryId);
}