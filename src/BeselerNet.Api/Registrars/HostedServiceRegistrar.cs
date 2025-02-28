using BeselerNet.Api.Accounts.Users;
using BeselerNet.Api.Communications;
using BeselerNet.Api.Core;
using BeselerNet.Api.Outbox;

namespace BeselerNet.Api.Registrars;

internal static class HostedServiceRegistrar
{
    public static void AddHostedServices(this IHostApplicationBuilder builder)
    {
        _ = builder.Services.AddHostedService<StartupService>();
        _ = builder.Services.AddHostedService<OutboxMonitor>();
        _ = builder.Services.AddHostedService<ForgotPasswordService>();
        _ = builder.Services.AddHostedService<SendGridEmailEventService>();
    }
}
