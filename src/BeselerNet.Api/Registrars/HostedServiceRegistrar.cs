using BeselerNet.Api.Accounts.Users;
using BeselerNet.Api.Communications;
using BeselerNet.Api.Outbox;

namespace BeselerNet.Api.Registrars;

internal static class HostedServiceRegistrar
{
    public static IHostApplicationBuilder AddHostedServices(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddHostedService<OutboxMonitor>()
            .AddHostedService<ForgotPasswordService>()
            .AddHostedService<MailjetEmailEventService>();

        return builder;
    }
}
