using System.Threading.Tasks;

namespace Scissors.Services;

public interface IAuthTokenRefreshService
{
    Task<bool> RefreshIfNeededAsync(bool force = false);
}
