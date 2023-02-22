using Axion.Collections.Concurrent;

namespace SimpleZ.SignalRManager.Abstractions;

public interface IConnectedUser
{
    IDictionary<string, Type> ConnectionIds { get; set; }
    IDictionary<string, ConcurrentHashSet<string>> Groups { get; set; }
}