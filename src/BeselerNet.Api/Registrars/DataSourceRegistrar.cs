using BeselerNet.Api.Accounts;
using Dapper;
using BeselerNet.Api.Outbox;
using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Communications;
using System.Data;
using BeselerNet.Api.Events;

namespace BeselerNet.Api.Registrars;

internal static class DataSourceRegistrar
{
    public static void AddDataSources(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDataSource("Database");

        _ = builder.Services.AddSingleton<OutboxDataSource>();
        _ = builder.Services.AddSingleton<EventLogDataSource>();
        _ = builder.Services.AddScoped<AccountDataSource>();
        _ = builder.Services.AddScoped<TokenLogDataSource>();
        _ = builder.Services.AddScoped<CommunicationDataSource>();

        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new StringUlidHandler());
    }

    public static void AddCaches(this IHostApplicationBuilder builder)
    {
        builder.AddRedisOutputCache("Cache");
        _ = builder.Services.AddMemoryCache();
        _ = builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Cache");
        });

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
