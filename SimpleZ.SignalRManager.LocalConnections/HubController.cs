using System.Collections.Concurrent;
using Axion.Collections.Concurrent;
using Nito.AsyncEx;
using SimpleZ.SignalRManager.Abstractions;
using SimpleZ.SignalRManager.Abstractions.Exceptions;
using SimpleZ.SignalRManager.LocalConnections.Models;

namespace SimpleZ.SignalRManager.LocalConnections;

public sealed class HubController<TId> : IHubController<TId> where TId : notnull
{
    private readonly IDictionary<TId, IConnectedUser> _connectedUsers;
    private readonly IDictionary<string, ConcurrentHashSet<string>> _groups;
    private readonly AsyncReaderWriterLock _readerWriterLock; 

    internal HubController()
    {
        _connectedUsers = new ConcurrentDictionary<TId, IConnectedUser>();
        _groups = new ConcurrentDictionary<string, ConcurrentHashSet<string>>();
        _readerWriterLock = new AsyncReaderWriterLock();
    }

    public string? IdClaimType { get; internal set; }
    public bool MultiHubConnection { get; internal set; }

    public bool MultiGroupConnection { get; internal set; }

    #region Public

    public async Task<ICollection<string>> GetGroupConnectionsAsync(string group)
    {
        using (await _readerWriterLock.ReaderLockAsync())
        {
            return await GetGroupConnectionsFromCacheAsync(group);
        }
    }

    public async Task<IConnectedUser?> GetConnectedUserAsync(TId userId)
    {
        using (await _readerWriterLock.ReaderLockAsync())
        {
            return await GetConnectedUserFromCacheAsync(userId);
        }
    }

    public int ActiveUsersCount => _connectedUsers.Count;

    public string? GetClientIdClaim => IdClaimType;

    public async Task<bool> UserExistsAsync(TId id)
    {
        using (await _readerWriterLock.ReaderLockAsync())
        {
            return await UserExistsInCacheAsync(id);
        }
    }

    public async Task<bool> GroupExistsAsync(string group)
    {
        using (await _readerWriterLock.ReaderLockAsync())
        {
            return await GroupExistsInCacheAsync(group);
        }
    }

    public async Task AddUserAsync(TId userId, string connectionId, Type hubContext)
    {
        using (await _readerWriterLock.ReaderLockAsync())
        {
            await AddUserInCacheAsync(userId, connectionId, hubContext);
        }
    }

    public async Task<IEnumerable<(string group, TId userId, string connectionId)>> RemoveUserAsync(TId userId,
        string connectionId, Type hubContext)
    {
        using (await _readerWriterLock.ReaderLockAsync())
        {
            return await RemoveUserFromCacheAsync(userId, connectionId, hubContext);
        }
    }

    public async Task AddUserToGroupAsync(string group, TId userId, string connectionId)
    {
        using (await _readerWriterLock.WriterLockAsync())
        {
            await AddUserToGroupCacheAsync(group, userId, connectionId);
        }
    }

    public async Task RemoveUserFromGroupAsync(string group, TId userId, string connectionId)
    {
        using (await _readerWriterLock.WriterLockAsync())
        {
            await RemoveUserFromGroupCacheAsync(group, userId, connectionId);
        }
    }

    public async Task ClearAllAsync()
    {
        using (await _readerWriterLock.WriterLockAsync())
        {
            await ClearAllCacheAsync();
        }
    }

    #endregion

    #region Private

    private Task<ICollection<string>> GetGroupConnectionsFromCacheAsync(string group) =>
        Task.FromResult(_groups[group] as ICollection<string>);
    
    private Task<IConnectedUser?> GetConnectedUserFromCacheAsync(TId userId)
    {
        if (_connectedUsers.ContainsKey(userId))
            return Task.FromResult(_connectedUsers[userId])!;
        
        return Task.FromResult<IConnectedUser?>(default);
    }
    
    private Task<bool> UserExistsInCacheAsync(TId id) => Task.FromResult(_connectedUsers.ContainsKey(id));

    private Task<bool> GroupExistsInCacheAsync(string group) => Task.FromResult(_groups.ContainsKey(group));
    
    private async Task AddUserInCacheAsync(TId userId, string connectionId, Type hubContext)
    {
        if (await UserExistsInCacheAsync(userId))
        {
            if (!MultiHubConnection &&
                _connectedUsers[userId].ConnectionIds!.Values.Contains(hubContext))
                throw new HubControllerException("User already joined. new connection is not allowed");

            IConnectedUser existingUser = _connectedUsers[userId];
            existingUser.ConnectionIds!.TryAdd(connectionId, hubContext);
            return;
        }

        ConnectedUser user = new ConnectedUser(){ ConnectionIds = new ConcurrentDictionary<string, Type>(), Groups = new ConcurrentDictionary<string, ConcurrentHashSet<string>>()};
        user.ConnectionIds.TryAdd(connectionId, hubContext);
        _connectedUsers.TryAdd(userId, user);
    }
    
    private async Task<IEnumerable<(string group, TId userId, string connectionId)>> RemoveUserFromCacheAsync(TId userId,
        string connectionId, Type hubContext)
    {
        IConnectedUser user = _connectedUsers[userId];
        var groups = new ConcurrentStack<(string group, TId userId, string connectionId)>();

        await Task.WhenAll(user.Groups!.Keys.Select(group => Task.Run(() => // ToDo: check out this
        {
            if (_groups.TryGetValue(group, out var connections) && connections.Contains(connectionId))
            {
                groups.Push((group, userId, connectionId));
                return RemoveUserFromGroupAsync(group, userId, connectionId);
            }

            return Task.CompletedTask;
        })));

        user.ConnectionIds!.Remove(connectionId); // Deleting connection in UserModel

        if (!user.ConnectionIds.Any())
            _connectedUsers.Remove(userId); // Deleting connection globally

        return groups;
    }
    
    private Task AddUserToGroupCacheAsync(string group, TId userId, string connectionId)
    {
        var userGroups = _connectedUsers[userId].Groups;

        if (!MultiGroupConnection && userGroups!.Keys.Contains(group))
            throw new HubControllerException(
                "This user already joined this group, several connections is not permitted");

        if (_groups.Keys.Contains(group))
        {
            _groups[group].Add(connectionId);
            if (userGroups!.Keys.Contains(group))
                userGroups[group].TryAdd(connectionId, out _);
            else
                userGroups.TryAdd(group, new ConcurrentHashSet<string>() { connectionId });

            return Task.CompletedTask;
        }

        _groups.TryAdd(group, new ConcurrentHashSet<string>() { connectionId });
        userGroups!.TryAdd(group, new ConcurrentHashSet<string>() { connectionId });
        return Task.CompletedTask;
    }
    
    private Task RemoveUserFromGroupCacheAsync(string group, TId userId, string connectionId)
    {
        if (!_connectedUsers.Keys.Contains(userId))
            return Task.CompletedTask; // ToDo: check out this
            
        _groups[group].Remove(connectionId);
        var userGroups = _connectedUsers[userId].Groups;
        userGroups![group].Remove(connectionId);

        if (!userGroups[group].Any())
            userGroups.Remove(group);
            
        if (!_groups[group].Any())
            _groups.Remove(group);
        
        return Task.CompletedTask;
    }
    
    private Task ClearAllCacheAsync()
    {
        _connectedUsers.Clear();
        _groups.Clear();
        return Task.CompletedTask;
    }

    #endregion
    
}