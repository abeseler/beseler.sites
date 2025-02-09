using Beseler.ServiceDefaults;
using BeselerNet.Api;
using BeselerNet.Api.Core;
using BeselerNet.Api.OAuth;
using BeselerNet.Api.Webhooks;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisOutputCache("Cache");
builder.AddNpgsqlDataSource("Database");
builder.Services.AddHttpContextAccessor();
builder.Services.AddRequestTimeouts();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHostedService<StartupService>();

builder.Services.AddScoped<Cookies>();

DefaultTypeMap.MatchNamesWithUnderscores = true;

var app = builder.Build();

app.UseExceptionHandler();
app.UseOutputCache();
app.MapOpenApi();

app.MapOAuthEndpoints();
app.MapWebhookEndpoints();
app.MapDefaultEndpoints();

app.Run();
