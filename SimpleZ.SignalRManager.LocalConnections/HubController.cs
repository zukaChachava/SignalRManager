using System.Collections.Concurrent;
using Axion.Collections.Concurrent;
using SimpleZ.SignalRManager.Abstractions;
using SimpleZ.SignalRManager.LocalConnections.Exceptions;
using SimpleZ.SignalRManager.LocalConnections.Models;
using Exception = System.Exception;

namespace SimpleZ.SignalRManager.LocalConnections;

public class HubController<TId>: IHubController<TId>
{
    private readonly IDictionary<TId, IConnectedUser> _connectedUsers;
    private readonly IDictionary<string, ConcurrentHashSet<string>> _groups;

    internal HubController()
    {
        _connectedUsers = new ConcurrentDictionary<TId, IConnectedUser>();
        _groups = new ConcurrentDictionary<string, ConcurrentHashSet<string>>();
    }
    
    public string IdClaimType { get; internal set; } 
    public bool MultiHubConnection { get; internal set; }
    
    public bool MultiGroupConnection { get; internal set; }
    

    public ICollection<string> this[string group] => _groups[group];

    public IConnectedUser? this[TId id]
    {
        get
        {
            if (_connectedUsers.ContainsKey(id))
                return _connectedUsers[id];
            return null;
        }
    }

    public int ActiveUsersCount => _connectedUsers.Count;

    public string GetClientIdClaim => IdClaimType;

    public bool ClientExists(TId id) => _connectedUsers.ContainsKey(id);

    public bool GroupExists(string group) => _groups.ContainsKey(group);

    public Task AddClientAsync(TId clientId, string connectionId, Type hubContext)
    {
        if (ClientExists(clientId))
        {
            if (!MultiHubConnection &&
                _connectedUsers[clientId].ConnectionIds.Values.Contains(hubContext))
                throw new HubControllerException("User already joined. new connection is not allowed");

            IConnectedUser existingUser = _connectedUsers[clientId];
            existingUser.ConnectionIds.TryAdd(connectionId, hubContext);
            return Task.CompletedTask;
        }

        ConnectedUser user = new ConnectedUser();
        user.ConnectionIds.TryAdd(connectionId, hubContext);
        _connectedUsers.TryAdd(clientId, user);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<(string group, TId userId, string connectionId)>> RemoveClientAsync(TId userId, string connectionId, Type hubContext)
    {
        throw new NotImplementedException();
    }

    public Task AddClientToGroupAsync(string group, TId clientId, string connectionId)
    {
        var userGroups = _connectedUsers[clientId].Groups;

        if (!MultiGroupConnection && userGroups.Keys.Contains(group))
            throw new HubControllerException(
                "This user already joined this group, several connections is not permitted");

        if (_groups.Keys.Contains(group))
        {
            _groups[group].Add(connectionId);
            if (userGroups.Keys.Contains(group))
                userGroups[group].Add(connectionId);
            else
                userGroups.TryAdd(group, new ConcurrentHashSet<string>() { connectionId });

            return Task.CompletedTask;
        }

        try
        {
            _groups.TryAdd(group, new ConcurrentHashSet<string>() { connectionId });
            userGroups.TryAdd(group, new ConcurrentHashSet<string>() { connectionId });
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // ToDo: log here
            throw;
        }
    }

    public Task RemoveClientFromGroupAsync(string group, TId clientId, string connectionId)
    {
        throw new NotImplementedException();
    }
}