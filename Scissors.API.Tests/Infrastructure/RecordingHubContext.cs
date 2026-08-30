using Microsoft.AspNetCore.SignalR;
using Scissors.API.Hub;

namespace Scissors.API.Tests.Infrastructure;

internal sealed class RecordingHubContext : IHubContext<ClippingsHub>
{
    public RecordingHubClients RecordingClients { get; } = new();

    public IHubClients Clients => RecordingClients;

    public IGroupManager Groups { get; } = new RecordingGroupManager();
}

internal sealed class RecordingHubClients : IHubClients
{
    public RecordingClientProxy Proxy { get; } = new();

    public string? LastUserId { get; private set; }

    public IClientProxy All => Proxy;
    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;
    public IClientProxy Caller => Proxy;
    public IClientProxy Client(string connectionId) => Proxy;
    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;
    public IClientProxy Group(string groupName) => Proxy;
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy;
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy;
    public IClientProxy Others => Proxy;
    public IClientProxy User(string userId)
    {
        LastUserId = userId;
        return Proxy;
    }
    public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
}

internal sealed class RecordingClientProxy : IClientProxy
{
    public List<(string Method, object?[] Args)> Calls { get; } = new();

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        Calls.Add((method, args));
        return Task.CompletedTask;
    }
}

internal sealed class RecordingGroupManager : IGroupManager
{
    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
