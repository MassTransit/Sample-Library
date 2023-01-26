#nullable enable
namespace Library.Components.Tests;

using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Quartz;


public static class LibraryTestConfigurationExtensions
{
    public static IServiceCollection ConfigureMassTransit(this IServiceCollection services, Action<IBusRegistrationConfigurator>? configure = null)
    {
        services.AddQuartz(x =>
            {
                x.UseMicrosoftDependencyInjectionJobFactory();
            })
            .AddMassTransitTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.AddQuartzConsumers();

                x.AddPublishMessageScheduler();

                configure?.Invoke(x);

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UsePublishMessageScheduler();
                        
                    cfg.ConfigureEndpoints(context);
                });
            });
            
        return services;
    }
}