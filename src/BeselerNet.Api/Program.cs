using Beseler.ServiceDefaults;
using BeselerNet.Api;
using BeselerNet.Api.Core;
using BeselerNet.Api.OAuth;
using BeselerNet.Api.Webhooks;
using Dapper;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisOutputCache("Cache");
builder.AddNpgsqlDataSource("Database");
builder.Services.AddHttpContextAccessor();
builder.Services.AddRequestTimeouts();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();
builder.Services.AddFusionCache()
    .WithOptions(o =>
    {
        o.CacheKeyPrefix = $"{builder.Environment.ApplicationName}:";
    })
    .WithSerializer(new FusionCacheSystemTextJsonSerializer())
    .WithDistributedCache(new RedisCache(new RedisCacheOptions() { Configuration = builder.Configuration["ConnectionStrings:Cache"] }))
    .WithBackplane(
        new RedisBackplane(new RedisBackplaneOptions() { Configuration = builder.Configuration["ConnectionStrings:Cache"] }))
    .AsHybridCache();

builder.Services.AddOpenTelemetry()
  .WithTracing(tracing => tracing.AddFusionCacheInstrumentation())
  .WithMetrics(metrics => metrics.AddFusionCacheInstrumentation());

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
