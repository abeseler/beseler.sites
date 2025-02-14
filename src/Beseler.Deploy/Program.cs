using Beseler.Deploy.Core;
using Beseler.Deploy.Deployments;
using Beseler.ServiceDefaults;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton(x => Channel.CreateUnbounded<WebhookRequest>(new()
{
    SingleReader = true,
    SingleWriter = false,
    AllowSynchronousContinuations = false
}));
builder.Services.AddHostedService<StartupService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddOptions<WebhookOptions>().BindConfiguration("Webhook");

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRequestLogging();

app.MapOpenApi();
app.MapDeployWebhook();
app.MapDefaultEndpoints();

app.Run();
