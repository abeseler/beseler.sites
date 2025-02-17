using Beseler.ServiceDefaults;
using BeselerNet.Api.Accounts;
using BeselerNet.Api.Core;
using BeselerNet.Api.Registrars;
using BeselerNet.Api.Webhooks;
using Microsoft.AspNetCore.Identity;
using SendGrid.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Telemetry.Source.Name));

builder.AddAuthentication();
builder.AddDataSources();
builder.AddCaches();
builder.AddHostedServices();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddRequestTimeouts();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<FeaturesOptions>().BindConfiguration(FeaturesOptions.SectionName);
builder.Services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName);
builder.Services.AddOptions<SendGridOptions>().BindConfiguration(SendGridOptions.SectionName);

builder.Services.AddSingleton<JwtGenerator>();
builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddScoped<Cookies>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddSingleton<DomainEventHandler>();
builder.Services.AddAccountDomainEventHandlers();

builder.Services.AddSendGrid(options =>
{
    var key = builder.Configuration.GetValue<string>("SendGrid:ApiKey");
    options.ApiKey = string.IsNullOrWhiteSpace(key) ? "MissingApiKey" : key;
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseRequestLogging();

app.MapOpenApi();
app.MapOAuthEndpoints();
app.MapWebhookEndpoints();
app.MapDefaultEndpoints();

app.Run();
