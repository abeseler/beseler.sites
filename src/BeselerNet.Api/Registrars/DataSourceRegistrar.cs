using BeselerNet.Api.Accounts;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
using ZiggyCreatures.Caching.Fusion;
using Dapper;
using BeselerNet.Api.Outbox;
using BeselerNet.Api.Accounts.OAuth;

namespace BeselerNet.Api.Registrars;

internal static class DataSourceRegistrar
{
    public static void AddDataSources(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDataSource("Database");

        builder.Services.AddSingleton<OutboxDataSource>();
        builder.Services.AddScoped<AccountDataSource>();
        builder.Services.AddScoped<TokenLogDataSource>();

        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public static void AddCaches(this IHostApplicationBuilder builder)
    {
        builder.AddRedisOutputCache("Cache");
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
    }
}
