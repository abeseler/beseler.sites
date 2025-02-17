using BeselerNet.Api.Accounts;
using BeselerNet.Api.Accounts.EventHandlers;
using System.Diagnostics;

namespace BeselerNet.Api.Core;

internal sealed class DomainEventHandler(IServiceProvider services)
{
    private readonly IServiceProvider _services = services;
    public Task Handle(DomainEvent domainEvent, CancellationToken stoppingToken)
    {
        using var scope = _services.CreateAsyncScope();
        using var activity = Telemetry.Source.StartActivity("DomainEventHandler.Handle", ActivityKind.Internal, domainEvent.TraceId);
        return domainEvent switch
        {
            AccountCreated e => scope.ServiceProvider.GetRequiredService<AccountCreatedHandler>().Handle(e, stoppingToken),
            AccountLoginFailed e => scope.ServiceProvider.GetRequiredService<AccountLoginFailedHandler>().Handle(e, stoppingToken),
            _ => throw new NotImplementedException($"No handler for {domainEvent.GetType().Name} event.")
        };
    }
}
