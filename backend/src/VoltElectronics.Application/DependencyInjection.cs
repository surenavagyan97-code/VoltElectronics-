using Microsoft.Extensions.DependencyInjection;
using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Use-case layer: the dispatchers plus every command, query and domain-event handler defined in
    /// this assembly. Nothing is listed by name — adding a handler is enough to register it.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddMessaging()
            .AddMessageHandlersFrom(typeof(DependencyInjection).Assembly)
            .AddCommandValidatorsFrom(typeof(DependencyInjection).Assembly);
}
