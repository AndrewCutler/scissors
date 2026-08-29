using System.Threading.Tasks;

namespace Scissors.Services;

public interface IClippingHubConnectionService
{
    Task StartAsync();
    Task StopAsync();
}
