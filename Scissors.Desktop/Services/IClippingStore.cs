using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public interface IClippingStore
{
    ReadOnlyObservableCollection<Clipping> Clippings { get; }
    void Init(List<Clipping> clippings);
    void Reset();
    void Add(Clipping clipping);
    void Remove(int id);
    void RemoveTemporary(Guid temporaryId);
}
