using Microsoft.Extensions.DependencyInjection;
using SimpleZ.SignalRManager.Abstractions;
using SimpleZ.SignalRManager.RedisConnections.Builder;

namespace SimpleZ.SignalRManager.RedisConnections.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddHubController<TId>(
        this IServiceCollection serviceCollection, 
        string connectionString,
        Action<IHubBuilder<TId>> config
        )
    {
        serviceCollection.AddSingleton<IHubController<TId>, HubController<TId>>(provider =>
        {
            HubBuilder<TId> builder = new HubBuilder<TId>(connectionString);
            config(builder);
            return (builder.Build() as HubController<TId>)!;
        });
    }
}