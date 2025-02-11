using Beseler.ServiceDefaults;
using BeselerNet.Api;
using BeselerNet.Api.Core;
using BeselerNet.Api.Identity;
using BeselerNet.Api.Identity.Models;
using BeselerNet.Api.Webhooks;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.AddServiceDefaults();
builder.AddAuthentication();
builder.AddNpgsqlDataSource("Database");
builder.AddRedisOutputCache("Cache");
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
    .WithBackplane(new RedisBackplane(new RedisBackplaneOptions() { Configuration = builder.Configuration["ConnectionStrings:Cache"] }))
    .AsHybridCache();

builder.Services.AddOpenTelemetry()
  .WithTracing(tracing => tracing.AddFusionCacheInstrumentation())
  .WithMetrics(metrics => metrics.AddFusionCacheInstrumentation());

builder.Services.AddHostedService<StartupService>();

builder.Services.AddScoped<Cookies>();
builder.Services.AddTransient<IPasswordHasher<Account>, PasswordHasher<Account>>();

DefaultTypeMap.MatchNamesWithUnderscores = true;

var app = builder.Build();

app.UseExceptionHandler();
app.UseRequestLogging();

app.MapOpenApi();
app.MapOAuthEndpoints();
app.MapWebhookEndpoints();
app.MapDefaultEndpoints();

app.Run();
