using System.Collections.Concurrent;
using Axion.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using SimpleZ.SignalRManager.Abstractions;
using SimpleZ.SignalRManager.Abstractions.Exceptions;
using SimpleZ.SignalRManager.RedisConnections.Models;
using StackExchange.Redis;

namespace SimpleZ.SignalRManager.RedisConnections;

[Authorize]
public class HubController<TId> : IHubController<TId>
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _usersDatabase;
    private readonly IDatabase _groupsDatabase;
    private readonly IDictionary<string, Type> _contextCache;
    private int _activeUsers;

    internal HubController(ConnectionMultiplexer redis, int usersDatabase, int groupsDatabase)
    {
        _redis = redis;
        _usersDatabase = _redis.GetDatabase(usersDatabase);
        _groupsDatabase = _redis.GetDatabase(groupsDatabase);
        _contextCache = new ConcurrentDictionary<string, Type>();
    }

    public string? IdClaimType { get; internal set; }
    public bool MultiHubConnection { get; internal set; }

    public bool MultiGroupConnection { get; internal set; }


    public ICollection<string> this[string group] => throw new NotImplementedException();

    public IConnectedUser? this[TId id] => throw new NotImplementedException();

    public int ActiveUsersCount => _activeUsers;
    public string? GetClientIdClaim { get; }

    public Task<bool> ClientExistsAsync(TId id) =>
        _usersDatabase.KeyExistsAsync(new RedisKey(id!.ToString()));

    public Task<bool> GroupExistsAsync(string group) =>
        _groupsDatabase.KeyExistsAsync(new RedisKey(group));

    public async Task AddClientAsync(TId userId, string connectionId, Type hubContext)
    {
        if (await ClientExistsAsync(userId))
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
                    new RedisValue(string.Join('/', group.Value))
                )).ToArray()
            )
        );

        if (!_contextCache.ContainsKey(hubContext.Name))
            _contextCache[hubContext.FullName!] = hubContext;
            
        Interlocked.Add(ref _activeUsers, 1);
    }

    public async Task<IEnumerable<(string group, TId userId, string connectionId)>> RemoveClientAsync(TId userId,
        string connectionId, Type hubContext)
    {
        var connectionsHashData =
            await _usersDatabase.HashGetAllAsync(new RedisKey($"{ConnectedUser.ConnectionIdsPrefix}_{userId}"));
        var userGroupsHashData =
            await _usersDatabase.HashGetAllAsync(new RedisKey($"{ConnectedUser.GroupsPrefix}_{userId}"));

        var groups = new ConcurrentStack<(string group, TId userId, string connectionId)>();

        IConnectedUser user = new ConnectedUser()
        {
            ConnectionIds = new ConcurrentDictionary<string, Type>(connectionsHashData.Select(data
                => new KeyValuePair<string, Type>(data.Name!, _contextCache[data.Value.ToString()]))), // ToDo: add type

            Groups = new ConcurrentDictionary<string, ConcurrentHashSet<string>>(userGroupsHashData.Select(data =>
                new KeyValuePair<string, ConcurrentHashSet<string>>(data.Name!,
                    new ConcurrentHashSet<string>(data.Value.ToString().Split('/')))))
        };


        await Task.WhenAll(user.Groups.Keys.Select(group => Task.Run(async () => // ToDo: check out this
        {
            if (await _groupsDatabase.KeyExistsAsync(group) && (await _groupsDatabase.StringGetAsync(group)).ToString()
                .Split('/').Contains(connectionId))
            {
                groups.Push((group, userId, connectionId));
                return RemoveClientFromGroupAsync(group, userId, connectionId);
            }

            return Task.CompletedTask;
        })));

        user.ConnectionIds.Remove(connectionId);

        if (!user.ConnectionIds.Any())
        {
            await Task.WhenAll(
                _usersDatabase.KeyDeleteAsync($"{ConnectedUser.ConnectionIdsPrefix}_{userId}"),
                _usersDatabase.KeyDeleteAsync($"{ConnectedUser.GroupsPrefix}_{userId}")
            );
            Interlocked.Add(ref _activeUsers, -1);
        }

        return groups;
    }

    public Task AddClientToGroupAsync(string group, TId clientId, string connectionId)
    {
        throw new NotImplementedException();
    }

    public Task RemoveClientFromGroupAsync(string group, TId clientId, string connectionId)
    {
        // ToDo: implement
        return Task.CompletedTask;
    }

    private async Task<bool> ContextEqualsAsync(TId userId, string context)
    {
        var contexts = await _usersDatabase.HashValuesAsync($"{ConnectedUser.ConnectionIdsPrefix}_{userId}");
        return contexts.SingleOrDefault(c => c.ToString() == context).HasValue;
    }
}