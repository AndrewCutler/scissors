using System.Collections.Generic;
using System.Threading.Tasks;
using Scissors.ViewModels;

public interface IClippingService
{
    Task<List<Clipping>> GetClippingsAsync();
    Task<Clipping> SaveClippingAsync(Clipping clipping);
    Task DeleteClippingAsync(Clipping clipping);
    void AddClipping(Clipping clipping);
}