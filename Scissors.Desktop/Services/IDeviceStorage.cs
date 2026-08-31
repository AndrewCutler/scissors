using System;
using System.Threading.Tasks;

public interface IDeviceStorage
{
    Task<Guid?> GetDeviceIdAsync();
    Task<Guid> SetDeviceIdAsync();
}
