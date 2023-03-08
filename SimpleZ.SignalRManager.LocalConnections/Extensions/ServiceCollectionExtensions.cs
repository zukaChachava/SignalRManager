using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SimpleZ.SignalRManager.Abstractions;
using SimpleZ.SignalRManager.LocalConnections.Builder;

namespace SimpleZ.SignalRManager.LocalConnections.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddHubController<TId>(this IServiceCollection serviceCollection, 
        Action<IHubBuilder<TId>> config)
    {
        string claimType = ClaimTypes.SerialNumber;
        
        serviceCollection.AddSingleton<IHubController<TId>, HubController<TId>>(provider =>
        {
            HubBuilder<TId> builder = new HubBuilder<TId>();
            config(builder);
            var controller = (builder.Build() as HubController<TId>)!;
            claimType = controller.IdClaimType!;
            return controller;
        });

        serviceCollection.AddSingleton<IUserIdProvider, ClaimProvider>(provider => new ClaimProvider(claimType));
    }
}