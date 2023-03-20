using Axion.Collections.Concurrent;
using SimpleZ.SignalRManager.Abstractions;

namespace SimpleZ.SignalRManager.RedisConnections.Models;

public class ConnectedUser : IConnectedUser
{
    public const string ConnectionIdsPrefix = "Connections";
    public const string GroupsPrefix = "Groups";
    
    public IDictionary<string, Type>? ConnectionIds { get; set; }
    public IDictionary<string, ConcurrentHashSet<string>>? Groups { get; set; }
}