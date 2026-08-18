using System.Threading.Tasks;
using Windows.Security.Credentials;

// TODO: some day, implement non-Windows specific versions too.
public class RefreshTokenStore : IRefreshTokenStore
{
    private const string Resource = "ScissorsDesktop";
    private const string Username = "refresh-token";

    public Task<string?> GetAsync()
    {
        var vault = new PasswordVault();

        try
        {
            var credential = vault.Retrieve(Resource, Username);
            credential.RetrievePassword();

            return Task.FromResult<string?>(credential.Password);
        }
        catch
        {
            // Credential doesn't exist
            return Task.FromResult<string?>(null);
    }
    }

    public Task SaveAsync(string refreshToken)
    {
        var vault = new PasswordVault();

        // Remove existing credential first
        try
        {
            var existing = vault.Retrieve(Resource, Username);
            vault.Remove(existing);
        }
        catch
        {
            // Doesn't exist yet
        }

        vault.Add(new PasswordCredential(
            Resource,
            Username,
            refreshToken));

        return Task.CompletedTask;
    }

    public Task DeleteAsync()
    {
        var vault = new PasswordVault();

        try
        {
            var credential = vault.Retrieve(Resource, Username);
            vault.Remove(credential);
        }
        catch
        {
            // Already gone
        }

        return Task.CompletedTask;
    }
}