namespace SimpleZ.SignalRManager.Abstractions;

public interface IHubController<TId>
{
    string? IdClaimType { get;}
    bool MultiHubConnection { get;}

    bool MultiGroupConnection { get; }
    ICollection<string> this[string group] { get; }
    IConnectedUser? this[TId id] { get; }
    int ActiveUsersCount { get; }
    Task<bool> UserExistsAsync(TId id);
    Task<bool> GroupExistsAsync(string group);
    Task AddUserAsync(TId userId, string connectionId, Type hubContext);
    Task<IEnumerable<(string group, TId userId, string connectionId)>> RemoveUserAsync(TId userId, string connectionId, Type hubContext);
    Task AddUserToGroupAsync(string group, TId userId, string connectionId);
    Task RemoveUserFromGroupAsync(string group, TId userId, string connectionId);
}