using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
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
        ) where TId : notnull
    {
        string claimType = ClaimTypes.SerialNumber;
        
        serviceCollection.AddSingleton<IHubController<TId>, HubController<TId>>(provider =>
        {
            HubBuilder<TId> builder = new HubBuilder<TId>(connectionString);
            config(builder);
            var controller = (builder.Build() as HubController<TId>)!;
            claimType = controller.IdClaimType!;
            return controller;
        });

        serviceCollection.AddSingleton<IUserIdProvider, ClaimProvider>(provider => new ClaimProvider(claimType));
    }
}