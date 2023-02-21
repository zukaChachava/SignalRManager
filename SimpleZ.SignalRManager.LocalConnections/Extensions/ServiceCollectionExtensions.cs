﻿using Microsoft.Extensions.DependencyInjection;
using SimpleZ.SignalRManager.Abstractions;
using SimpleZ.SignalRManager.LocalConnections.Builder;

namespace SimpleZ.SignalRManager.LocalConnections.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddHubController<TId>(this IServiceCollection serviceCollection, 
        Action<IHubBuilder<TId>> config)
    {
        serviceCollection.AddSingleton<IHubController<TId>, HubController<TId>>(provider =>
        {
            HubBuilder<TId> builder = new HubBuilder<TId>();
            config(builder);
            
            return  (builder.Build() as HubController<TId>)!;
        });
    }
}