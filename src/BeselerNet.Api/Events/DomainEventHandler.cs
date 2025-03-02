using BeselerNet.Api.Accounts;
using BeselerNet.Api.Core;
using BeselerNet.Api.Events.Handlers;
using System.Diagnostics;

namespace BeselerNet.Api.Events;

internal sealed class DomainEventHandler(IServiceScopeFactory scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    public Task Handle(DomainEvent domainEvent, CancellationToken stoppingToken)
    {
        using var activity = Telemetry.Source.StartActivity("DomainEventHandler.Handle", ActivityKind.Internal, domainEvent.TraceId);
        using var scope = _scopeFactory.CreateScope();
        return domainEvent switch
        {
            AccountCreated e => scope.ServiceProvider.GetRequiredService<AccountCreatedHandler>().Handle(e, stoppingToken),
            AccountLoginFailed e => scope.ServiceProvider.GetRequiredService<AccountLoginFailedHandler>().Handle(e, stoppingToken),
            _ => throw new NotImplementedException($"No handler for {domainEvent.GetType().Name} event.")
        };
    }
}

internal static class DomainEventHandlerRegistrar
{
    public static void AddDomainEventHandlers(this IServiceCollection services)
    {
        _ = services.AddSingleton<DomainEventHandler>()
            .AddTransient<AccountCreatedHandler>()
            .AddTransient<AccountLoginFailedHandler>();
    }
}
