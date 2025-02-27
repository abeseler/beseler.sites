using BeselerNet.Api.Accounts.Users;
using BeselerNet.Api.Core;
using BeselerNet.Api.Outbox;

namespace BeselerNet.Api.Registrars;

internal static class HostedServiceRegistrar
{
    public static void AddHostedServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHostedService<StartupService>();
        builder.Services.AddHostedService<OutboxMonitor>();
        builder.Services.AddHostedService<ForgotPasswordService>();
    }
}
