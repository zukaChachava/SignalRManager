using System.Collections.Concurrent;
using Axion.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using SimpleZ.SignalRManager.Abstractions;
using SimpleZ.SignalRManager.Abstractions.Exceptions;
using SimpleZ.SignalRManager.RedisConnections.Models;
using StackExchange.Redis;

namespace SimpleZ.SignalRManager.RedisConnections;

/*
 * User Connections using two types of storage:
 *  1. userId -> hash(connectionId, hubType)
 *  2. userId -> hash(group, connectionIds) /connectionIds is just string seperated with "/" symbol.
 *
 * Groups using one type of storage:
 *  1. group -> set(connectionId, connectionId, ...)
 */

[Authorize]
public sealed class HubController<TId> : IHubController<TId>
{
    private const char Separator = '/';
    private readonly IDatabase _usersDatabase;
    private readonly IDatabase _groupsDatabase;
    private readonly IDictionary<string, Type> _contextCache;
    private readonly SemaphoreSlim _semaphoreSlim;
    private int _activeUsers;

    internal HubController(ConnectionMultiplexer redis, int usersDatabase, int groupsDatabase)
    {
        _usersDatabase = redis.GetDatabase(usersDatabase);
        _groupsDatabase = redis.GetDatabase(groupsDatabase);
        _contextCache = new ConcurrentDictionary<string, Type>();
        _semaphoreSlim = new SemaphoreSlim(1);
    }

    public string? IdClaimType { get; internal set; }
    public bool MultiHubConnection { get; internal set; }

    public bool MultiGroupConnection { get; internal set; }

    public async Task<ICollection<string>> GetGroupConnectionsAsync(string group)
    {
        var members = await _groupsDatabase.SetMembersAsync(group);
        return members.Select(member => member.ToString()).ToArray();
    }

    public async Task<IConnectedUser> GetConnectedUserAsync(TId userId)
    {
        var userConnections =
            await _usersDatabase.HashGetAllAsync(new RedisKey($"{ConnectedUser.ConnectionIdsPrefix}_{userId}"));
        var userGroups = 
            await _usersDatabase.HashGetAllAsync(new RedisKey($"{ConnectedUser.GroupsPrefix}_{userId}"));

        var restoredUserConnections =
            new ConcurrentDictionary<string, Type>(
                userConnections.Select(connection =>
                    new KeyValuePair<string, Type>(connection.Name.ToString(),
                        _contextCache[connection.Value.ToString()])));

        var restoredUserGroups = 
            new ConcurrentDictionary<string, ConcurrentHashSet<string>>(
                userGroups.Select(group => new KeyValuePair<string, ConcurrentHashSet<string>>(
                    group.Name.ToString(),
                    new ConcurrentHashSet<string>(group.Value.ToString().Split(Separator))
                    ))
                );

        return new ConnectedUser() { ConnectionIds = restoredUserConnections, Groups = restoredUserGroups };
    }

    public int ActiveUsersCount => _activeUsers;

    public Task<bool> UserExistsAsync(TId id) =>
        _usersDatabase.KeyExistsAsync(new RedisKey(id!.ToString()));

    public Task<bool> GroupExistsAsync(string group) =>
        _groupsDatabase.KeyExistsAsync(new RedisKey(group));

    public async Task AddUserAsync(TId userId, string connectionId, Type hubContext)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();
            await AddUserToRedisAsync(userId, connectionId, hubContext);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<IEnumerable<(string group, TId userId, string connectionId)>> RemoveUserAsync(TId userId,
        string connectionId, Type hubContext)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();
            return await RemoveUserFromRedisAsync(userId, connectionId, hubContext);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task AddUserToGroupAsync(string group, TId userId, string connectionId)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();
            await AddUserToRedisGroupAsync(group, userId, connectionId);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task RemoveUserFromGroupAsync(string group, TId userId, string connectionId)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();
            await RemoveUserFromRedisGroupAsync(group, userId, connectionId);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private async Task<bool> ContextEqualsAsync(TId userId, string context)
    {
        var contexts = await _usersDatabase.HashValuesAsync($"{ConnectedUser.ConnectionIdsPrefix}_{userId}");
        return contexts.SingleOrDefault(c => c.ToString() == context).HasValue;
    }

    private async Task AddUserToRedisAsync(TId userId, string connectionId, Type hubContext)
    {
        if (await UserExistsAsync(userId))
        {
            if (!MultiHubConnection && await ContextEqualsAsync(userId, hubContext.Name))
                throw new HubControllerException("User already joined. new connection is not allowed");

            await _usersDatabase.HashSetAsync($"{ConnectedUser.ConnectionIdsPrefix}_{userId}", new HashEntry[]
            {
                new HashEntry(new RedisValue(connectionId), new RedisValue(hubContext.Name))
            });

            return;
        }

        IConnectedUser user = new ConnectedUser()
        {
            ConnectionIds = new ConcurrentDictionary<string, Type>(),
            Groups = new ConcurrentDictionary<string, ConcurrentHashSet<string>>()
        };

        user.ConnectionIds.TryAdd(connectionId, hubContext);

        await Task.WhenAll(
            _usersDatabase.HashSetAsync(
                new RedisKey($"{ConnectedUser.ConnectionIdsPrefix}_{userId}"),
                user.ConnectionIds.Select(connection => new HashEntry(
                    new RedisValue(connection.Key),
                    new RedisValue(connection.Value.ToString())
                )).ToArray()
            ),
            _usersDatabase.HashSetAsync(
                new RedisKey($"{ConnectedUser.GroupsPrefix}_{userId}"),
                user.Groups.Select(group => new HashEntry(
                    new RedisValue(group.Key),
                    new RedisValue(string.Join(Separator, group.Value))
                )).ToArray()
            )
        );

        if (!_contextCache.ContainsKey(hubContext.Name))
            _contextCache[hubContext.FullName!] = hubContext;

        Interlocked.Add(ref _activeUsers, 1);
    }

    private async Task<IEnumerable<(string group, TId userId, string connectionId)>> RemoveUserFromRedisAsync(
        TId userId,
        string connectionId, Type hubContext)
    {
        var connectionsHashData =
            await _usersDatabase.HashGetAllAsync(new RedisKey($"{ConnectedUser.ConnectionIdsPrefix}_{userId}"));
        var userGroupsHashData =
            await _usersDatabase.HashGetAllAsync(new RedisKey($"{ConnectedUser.GroupsPrefix}_{userId}"));

        IConnectedUser user = new ConnectedUser()
        {
            ConnectionIds = new ConcurrentDictionary<string, Type>(connectionsHashData.Select(data
                => new KeyValuePair<string, Type>(data.Name!,
                    _contextCache[data.Value.ToString()]))), // ToDo: add type

            Groups = new ConcurrentDictionary<string, ConcurrentHashSet<string>>(userGroupsHashData.Select(data =>
                new KeyValuePair<string, ConcurrentHashSet<string>>(data.Name!,
                    new ConcurrentHashSet<string>(data.Value.ToString().Split('/')))))
        };

        var connectedGroups = user
            .Groups
            .Where(group => group.Value.Contains(connectionId))
            .ToArray();

        await Task.WhenAll(connectedGroups.Select(group => 
            RemoveUserFromRedisGroupAsync(group.Key, userId, connectionId, new RedisValue(string.Join(Separator, group.Value)))));
        user.ConnectionIds.Remove(connectionId);

        await Task.WhenAll(
            _usersDatabase.KeyDeleteAsync($"{ConnectedUser.ConnectionIdsPrefix}_{userId}"),
            _usersDatabase.KeyDeleteAsync($"{ConnectedUser.GroupsPrefix}_{userId}")
        );

        if (user.ConnectionIds.Any())
        {
           await _usersDatabase.HashSetAsync(
                new RedisKey($"{ConnectedUser.ConnectionIdsPrefix}_{userId}"),
                user.ConnectionIds.Select(connection => new HashEntry(
                    new RedisValue(connection.Key),
                    new RedisValue(connection.Value.ToString())
                )).ToArray()
            );
        }
        else
        {
            Interlocked.Add(ref _activeUsers, -1);
        }

        return connectedGroups.Select(group => (group.Key, userId, connectionId));
    }

    private async Task AddUserToRedisGroupAsync(string group, TId userId, string connectionId)
    {
        var redisGroupKey = new RedisKey($"{ConnectedUser.GroupsPrefix}_{userId}");
        var userGroups = await _usersDatabase.HashGetAllAsync(redisGroupKey);

        if (!userGroups.Any())
        {
            await _usersDatabase.HashSetAsync(redisGroupKey, new HashEntry[]
            {
                new HashEntry(new RedisValue(group), new RedisValue(connectionId))
            });
        }
        else
        {
            var userGroup = userGroups.SingleOrDefault(hashEntry => hashEntry.Name.ToString() == group);

            if (!userGroup.Value.IsNullOrEmpty && !MultiGroupConnection)
                throw new HubControllerException(
                    "This user already joined this group, several connections is not permitted");

            await _usersDatabase
                .HashSetAsync(
                    redisGroupKey,
                    new HashEntry[]
                        { new HashEntry(group, string.Join(Separator, userGroup.Value.ToString(), connectionId)) }
                );
        }

        await _groupsDatabase.SetAddAsync(new RedisKey(group), new RedisValue(connectionId));
    }

    private async Task RemoveUserFromRedisGroupAsync(string group, TId userId, string connectionId, RedisValue? groupConnections = null)
    {
        var hashKey = new RedisKey($"{ConnectedUser.GroupsPrefix}_{userId}");
        var userGroup = groupConnections ?? await _usersDatabase.HashGetAsync(hashKey, new RedisValue(connectionId));

        if (userGroup.IsNullOrEmpty)
            return;

        await _usersDatabase.HashDeleteAsync(hashKey, group);

        var connectionIds = userGroup.ToString().Split(Separator).Where(connection => connection != connectionId)
            .ToArray();

        if (connectionIds.Any())
            await _usersDatabase.HashSetAsync(hashKey,
                new HashEntry[] { new HashEntry(group, string.Join(Separator, connectionIds)) });

        await _groupsDatabase.SetRemoveAsync(new RedisKey(group), new RedisValue(connectionId));
    }
}