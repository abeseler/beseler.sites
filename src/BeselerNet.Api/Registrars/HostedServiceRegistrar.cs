namespace BeselerNet.Api.Registrars;

internal static class HostedServiceRegistrar
{
    public static void AddHostedServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHostedService<StartupService>();
    }
}
