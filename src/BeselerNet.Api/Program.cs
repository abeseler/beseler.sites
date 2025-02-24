using Beseler.ServiceDefaults;
using BeselerNet.Api.Accounts;
using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Accounts.Users;
using BeselerNet.Api.Communications;
using BeselerNet.Api.Core;
using BeselerNet.Api.Events;
using BeselerNet.Api.Registrars;
using BeselerNet.Api.Webhooks;
using Cysharp.Serialization.Json;
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

builder.Services.AddOpenApi(options =>
{
    _ = options.AddDocumentTransformer<OpenApiDefaultTransformer>();
    _ = options.AddDocumentTransformer<AuthenticationSchemeTransformer>();
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new UlidJsonConverter());
});
builder.Services.AddProblemDetails();
builder.Services.AddRequestTimeouts();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<OpenApiOptions>().BindConfiguration(OpenApiOptions.SectionName);
builder.Services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName);
builder.Services.AddOptions<SendGridOptions>().BindConfiguration(SendGridOptions.SectionName);

builder.Services.AddSingleton<JwtGenerator>();
builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddScoped<Cookies>();
builder.Services.AddScoped<SendGridEmailService>();

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

app.MapOpenApi().CacheOutput();
app.MapOAuthEndpoints();
app.MapUserAccountEndpoints();
app.MapWebhookEndpoints();
app.MapDefaultEndpoints();

app.Run();
