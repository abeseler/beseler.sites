using BeselerNet.Api.Communications;
using BeselerNet.Api.Communications.Emails;
using Mailjet.Client;

namespace BeselerNet.Api.Registrars;

internal static class EmailProviderRegistrar
{
    public static IHostApplicationBuilder AddEmailProvider(this IHostApplicationBuilder builder, Action<EmailProviderOptions>? options = null)
    {
        builder.Services.AddScoped<CommunicationService>();
        builder.Services.AddOptions<AzureOptions>().BindConfiguration(AzureOptions.SectionName);
        builder.Services.AddOptions<MailjetOptions>().BindConfiguration(MailjetOptions.SectionName);

        var config = new EmailProviderOptions();
        options?.Invoke(config);

        switch (config.Provider)
        {
            case EmailProvider.Azure:
                builder.Services.AddScoped<IEmailClient, AzureEmailClient>();
                break;
            case EmailProvider.Mailjet:
                builder.Services.AddScoped<IEmailClient, MailjetEmailClient>();
                builder.Services.AddHttpClient<IMailjetClient, MailjetClient>(client =>
                {
                    client.SetDefaultSettings();
                    var key = builder.Configuration.GetValue<string>("Mailjet:ApiKey");
                    var secret = builder.Configuration.GetValue<string>("Mailjet:ApiSecret");
                    client.UseBasicAuthentication(key, secret);
                });
                break;
        }

        return builder;
    }
}

internal sealed record EmailProviderOptions
{
    public EmailProvider Provider { get; set; } = EmailProvider.Azure;
}

internal enum EmailProvider
{
    Azure,
    Mailjet
}
