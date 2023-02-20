namespace SimpleZ.SignalRManager.Abstractions;

public interface IHubController<TId>
{
    ICollection<string> this[string group] { get; }
    IConnectedUser? this[TId id] { get; }
    int ActiveUsersCount { get; }
    string GetClientIdClaim { get; }
    bool ClientExists(TId id);
    bool GroupExists(string group);
    Task AddClientAsync(TId clientId, string connectionId, Type hubContext);
    Task<IEnumerable<(string group, TId userId, string connectionId)>> RemoveClientAsync(TId userId, string connectionId, Type hubContext);
    Task AddClientToGroupAsync(string group, TId clientId, string connectionId);
    Task RemoveClientFromGroupAsync(string group, TId clientId, string connectionId);
}