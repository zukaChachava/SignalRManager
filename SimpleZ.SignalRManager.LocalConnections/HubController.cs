using System.Collections.Concurrent;
using Axion.Collections.Concurrent;
using SimpleZ.SignalRManager.Abstractions;
using SimpleZ.SignalRManager.LocalConnections.Exceptions;
using SimpleZ.SignalRManager.LocalConnections.Models;

namespace SimpleZ.SignalRManager.LocalConnections;

public class HubController<TId> : IHubController<TId>
{
    private readonly IDictionary<TId, IConnectedUser> _connectedUsers;
    private readonly IDictionary<string, ConcurrentHashSet<string>> _groups;

    internal HubController()
    {
        _connectedUsers = new ConcurrentDictionary<TId, IConnectedUser>();
        _groups = new ConcurrentDictionary<string, ConcurrentHashSet<string>>();
    }

    public string? IdClaimType { get; internal set; }
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

    public string? GetClientIdClaim => IdClaimType;

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

    public async Task<IEnumerable<(string group, TId userId, string connectionId)>> RemoveClientAsync(TId userId,
        string connectionId, Type hubContext)
    {
        IConnectedUser client = _connectedUsers[userId];
        var groups = new ConcurrentStack<(string group, TId userId, string connectionId)>();

        await Task.WhenAll(client.Groups.Keys.Select(group => Task.Run(() => // ToDo: check out this
        {
            if (_groups.TryGetValue(group, out var connections) && connections.Contains(connectionId))
            {
                groups.Push((group, userId, connectionId));
                return RemoveClientFromGroupAsync(group, userId, connectionId);
            }

            return Task.CompletedTask;
        })));

        client.ConnectionIds.Remove(connectionId); // Deleting connection in UserModel

        if (!client.ConnectionIds.Any())
            _connectedUsers.Remove(userId); // Deleting connection globally

        return groups;
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

        _groups.TryAdd(group, new ConcurrentHashSet<string>() { connectionId });
        userGroups.TryAdd(group, new ConcurrentHashSet<string>() { connectionId });
        return Task.CompletedTask;
    }

    public Task RemoveClientFromGroupAsync(string group, TId clientId, string connectionId)
    {
        if (!_connectedUsers.Keys.Contains(clientId))
            return Task.CompletedTask; // ToDo: check out this
            
        _groups[group].Remove(connectionId);
        var userGroups = _connectedUsers[clientId].Groups;
        userGroups[group].Remove(connectionId);

        if (!userGroups[group].Any())
            userGroups.Remove(group);
            
        if (!_groups[group].Any())
            _groups.Remove(group);
        
        return Task.CompletedTask;
    }
}