#nullable enable
namespace Library.Integration.Tests;

using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Quartz;


public static class LibraryTestConfigurationExtensions
{
    public static IServiceCollection ConfigureMassTransit(this IServiceCollection services, Action<IBusRegistrationConfigurator>? configure = null)
    {
        services
            .AddDbContext<IntegrationTestDbContext>(x =>
            {
                IntegrationTestSagaDbContextFactory.Apply(x);
            })
            .AddQuartz(x =>
            {
                x.UseMicrosoftDependencyInjectionJobFactory();
            })
            .AddHostedService<MigrationHostedService<IntegrationTestDbContext>>()
            .AddMassTransitTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.SetEntityFrameworkSagaRepositoryProvider(r =>
                {
                    r.UsePostgres();
                    r.ExistingDbContext<IntegrationTestDbContext>();
                });

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