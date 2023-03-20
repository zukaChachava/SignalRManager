using Axion.Collections.Concurrent;
using SimpleZ.SignalRManager.Abstractions;

namespace SimpleZ.SignalRManager.LocalConnections.Models;

internal class ConnectedUser : IConnectedUser
{
    public IDictionary<string, Type>? ConnectionIds { get; set; }
    public IDictionary<string, ConcurrentHashSet<string>>? Groups { get; set; }
}