using BeselerNet.Api.Accounts;
using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Communications;
using BeselerNet.Api.Events;
using BeselerNet.Api.Outbox;
using Dapper;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using System.Data;

namespace BeselerNet.Api.Registrars;

internal static class DataSourceRegistrar
{
    public static void AddDataSources(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDataSource("Database");

        _ = builder.Services
            .AddScoped<AccountDataSource>()
            .AddScoped<CommunicationDataSource>()
            .AddSingleton<EventLogDataSource>()
            .AddSingleton<OutboxDataSource>()
            .AddSingleton<PermissionDataSource>()
            .AddScoped<TokenLogDataSource>();

        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new StringUlidHandler());
    }

    public static void AddCaches(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Cache");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var connection = ConnectionMultiplexer.Connect(connectionString);
            _ = builder.Services.AddSingleton<IConnectionMultiplexer>(connection);

            _ = builder.Services.AddDataProtection()
                .SetApplicationName(builder.Environment.ApplicationName)
                .PersistKeysToStackExchangeRedis(connection, "DataProtection-Keys");

            _ = builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = "Api:";
            });

            builder.AddRedisOutputCache("Cache");
        }

        _ = builder.Services.AddMemoryCache();

#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        _ = builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018
    }
}

internal sealed class StringUlidHandler : SqlMapper.TypeHandler<Ulid>
{
    public override Ulid Parse(object value)
    {
        return Ulid.Parse((string)value);
    }

    public override void SetValue(IDbDataParameter parameter, Ulid value)
    {
        parameter.DbType = DbType.StringFixedLength;
        parameter.Size = 26;
        parameter.Value = value.ToString();
    }
}
