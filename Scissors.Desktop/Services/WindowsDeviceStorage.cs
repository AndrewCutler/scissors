using System;
using System.IO;
using System.Threading.Tasks;

public class WindowsDeviceStorage : IDeviceStorage
{
    private static readonly string _appFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Scissors");

    private static readonly string _deviceIdPath =
        Path.Combine(_appFolder, "device-id.txt");

    public async Task<Guid?> GetDeviceIdAsync()
    {
        if (!File.Exists(_deviceIdPath))
        {
            return null;
        }

        var text = await File.ReadAllTextAsync(_deviceIdPath);

        if (!Guid.TryParse(text, out var guid))
        {
            // log
            return null;
        }

        return guid;
    }

    public async Task<Guid> SetDeviceIdAsync()
    {
        var deviceId = Guid.NewGuid();

        Directory.CreateDirectory(_appFolder);
        await File.WriteAllTextAsync(_deviceIdPath, deviceId.ToString());

        return deviceId;
    }
}
