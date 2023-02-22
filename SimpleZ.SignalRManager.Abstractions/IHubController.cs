namespace SimpleZ.SignalRManager.Abstractions;

public interface IHubController<TId>
{
    string? IdClaimType { get;}
    bool MultiHubConnection { get;}

    bool MultiGroupConnection { get; }
    ICollection<string> this[string group] { get; }
    IConnectedUser? this[TId id] { get; }
    int ActiveUsersCount { get; }
    Task<bool> ClientExistsAsync(TId id);
    Task<bool> GroupExistsAsync(string group);
    Task AddClientAsync(TId userId, string connectionId, Type hubContext);
    Task<IEnumerable<(string group, TId userId, string connectionId)>> RemoveClientAsync(TId userId, string connectionId, Type hubContext);
    Task AddClientToGroupAsync(string group, TId clientId, string connectionId);
    Task RemoveClientFromGroupAsync(string group, TId clientId, string connectionId);
}