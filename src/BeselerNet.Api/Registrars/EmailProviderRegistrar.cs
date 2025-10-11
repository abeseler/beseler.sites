using BeselerNet.Api.Communications;
using Mailjet.Client;

namespace BeselerNet.Api.Registrars;

internal static class EmailProviderRegistrar
{
    public static IHostApplicationBuilder AddEmailProviders(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<Emailer>();

        //Mailjet implementation
        builder.Services.AddScoped<IEmailClient, MailjetEmailClient>();
        builder.Services.AddOptions<MailjetOptions>().BindConfiguration(MailjetOptions.SectionName);
        builder.Services.AddHttpClient<IMailjetClient, MailjetClient>(client =>
        {
            client.SetDefaultSettings();
            var key = builder.Configuration.GetValue<string>("Mailjet:ApiKey");
            var secret = builder.Configuration.GetValue<string>("Mailjet:ApiSecret");
            client.UseBasicAuthentication(key, secret);
        });

        //Azure implementation
        //builder.Services.AddScoped<IEmailClient, AzureEmailClient>();
        builder.Services.AddOptions<AzureOptions>().BindConfiguration(AzureOptions.SectionName);

        return builder;
    }
}
