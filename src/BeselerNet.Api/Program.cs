using Beseler.ServiceDefaults;
using BeselerNet.Api.Accounts;
using BeselerNet.Api.Accounts.EventHandlers;
using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Communications;
using BeselerNet.Api.Core;
using BeselerNet.Api.OpenApi;
using BeselerNet.Api.Outbox;
using BeselerNet.Api.Registrars;
using BeselerNet.Api.Webhooks;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Telemetry.Source.Name));

builder.AddAuthentication();
builder.AddDataSources();
builder.AddCaching();
builder.AddHostedServices();

builder.Services.AddOptions<OpenApiOptions>().BindConfiguration(OpenApiOptions.SectionName);
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<OpenApiDefaultTransformer>();
    options.AddDocumentTransformer<AuthenticationSchemeTransformer>();
});
builder.Services.AddProblemDetails();
builder.Services.AddRequestTimeouts();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName);
builder.Services.AddSingleton<JwtGenerator>();
builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddScoped<Cookies>();

builder.Services.AddSingleton<OutboxMessageProcessor>();
builder.Services.AddSingleton<DomainEventDispatcher>();

builder.Services.AddScoped<IHandler<AccountCreated>, AccountCreatedHandler>();
builder.Services.AddScoped<IHandler<AccountLoginFailed>, AccountLoginFailedHandler>();

builder.Services.AddOptions<CommunicationOptions>().BindConfiguration(CommunicationOptions.SectionName);
builder.AddEmailProviders();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRequestLogging();

app.MapOpenApi().CacheOutput();
app.MapDefaultEndpoints();
app.MapAccountEndpoints();
app.MapWebhookEndpoints();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "API V1");
});

app.Run();
