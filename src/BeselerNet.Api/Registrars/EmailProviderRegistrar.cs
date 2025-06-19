using BeselerNet.Api.Communications;
using Mailjet.Client;
using SendGrid.Extensions.DependencyInjection;

namespace BeselerNet.Api.Registrars;

internal static class EmailProviderRegistrar
{
    public static IHostApplicationBuilder AddEmailProviders(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<EmailerProvider>();
        builder.AddSendGrid();
        builder.AddMailjet();

        return builder;
    }

    private static void AddSendGrid(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<SendGridOptions>().BindConfiguration(SendGridOptions.SectionName);
        builder.Services.AddScoped<SendGridEmailService>();
        builder.Services.AddSendGrid(options =>
        {
            var key = builder.Configuration.GetValue<string>("SendGrid:ApiKey");
            options.ApiKey = string.IsNullOrWhiteSpace(key) ? "MissingApiKey" : key;
        });
    }

    private static void AddMailjet(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<MailjetOptions>().BindConfiguration(MailjetOptions.SectionName);
        builder.Services.AddScoped<MailjetEmailService>();
        builder.Services.AddHttpClient<IMailjetClient, MailjetClient>(client =>
        {
            client.SetDefaultSettings();
            var key = builder.Configuration.GetValue<string>("Mailjet:ApiKey");
            var secret = builder.Configuration.GetValue<string>("Mailjet:ApiSecret");
            client.UseBasicAuthentication(key, secret);
        });
    }
}
