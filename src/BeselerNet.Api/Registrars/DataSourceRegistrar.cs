using BeselerNet.Api.Accounts;
using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Communications;
using BeselerNet.Api.Events;
using BeselerNet.Api.Outbox;
using Dapper;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

namespace BeselerNet.Api.Registrars;

internal static class DataSourceRegistrar
{
    public static IHostApplicationBuilder AddDataSources(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDataSource("Database");
        builder.Services
            .AddScoped<AccountDataSource>()
            .AddScoped<CommunicationDataSource>()
            .AddSingleton<OutboxDataSource>()
            .AddSingleton<EventLogDataSource>()
            .AddSingleton<PermissionDataSource>()
            .AddScoped<TokenLogDataSource>();

        DefaultTypeMap.MatchNamesWithUnderscores = true;

        return builder;
    }

    public static IHostApplicationBuilder AddCaching(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Cache");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var connection = ConnectionMultiplexer.Connect(connectionString);
            builder.Services.AddSingleton<IConnectionMultiplexer>(connection);

            builder.Services.AddDataProtection()
                .SetApplicationName(builder.Environment.ApplicationName)
                .PersistKeysToStackExchangeRedis(connection, "DataProtection-Keys");

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = "Api:";
            });

            builder.AddRedisOutputCache("Cache");
        }

        builder.Services.AddMemoryCache();
        builder.Services.AddHybridCache();

        return builder;
    }
}

