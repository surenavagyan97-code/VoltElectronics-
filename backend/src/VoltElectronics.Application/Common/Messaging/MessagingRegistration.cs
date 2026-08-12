using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace VoltElectronics.Application.Common.Messaging;

public static class MessagingRegistration
{
    /// <summary>
    /// Registers the dispatchers. Handlers themselves are picked up per-assembly by
    /// <see cref="AddMessageHandlersFrom"/> — commands live in Application, read projections live
    /// in the persistence layer, and both need scanning.
    /// </summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return services;
    }

    /// <summary>
    /// Scans one assembly for command, query and domain-event handlers and binds each to the exact
    /// closed handler interface it implements — never <c>AsImplementedInterfaces</c>, which would
    /// also publish incidental interfaces like <c>IDisposable</c>.
    /// </summary>
    public static IServiceCollection AddMessageHandlersFrom(this IServiceCollection services, Assembly assembly)
    {
        foreach (var contract in new[] { typeof(ICommandHandler<,>), typeof(IQueryHandler<,>), typeof(IDomainEventHandler<>) })
        {
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(c => c.AssignableTo(contract), publicOnly: false)
                .AsImplementedInterfaces(i => i.IsGenericType && i.GetGenericTypeDefinition() == contract)
                .WithScopedLifetime());
        }

        return services;
    }
}
