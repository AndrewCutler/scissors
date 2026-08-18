using System.Threading.Tasks;

public interface IRefreshTokenStore
{
    Task<string?> GetAsync();
    Task SaveAsync(string refreshToken);
    Task DeleteAsync();
}