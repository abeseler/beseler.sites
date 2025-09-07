using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

namespace Beseler.ServiceDefaults;

public sealed record ServiceDefaultsOptions
{
    public bool UseDefaultStartupService { get; init; } = true;
}

public static class Extensions
{
    private const string HealthEndpoint = "/_health";
    private const string AliveEndpoint = "/_alive";
    private const string ReadyEndpoint = "/_ready";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder, Action<ServiceDefaultsOptions>? options = null) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        var opts = new ServiceDefaultsOptions();
        options?.Invoke(opts);
        if (opts.UseDefaultStartupService)
        {
            builder.Services.AddHostedService<DefaultStartupService>();
        }

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(HealthEndpoint, new() { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse })
            .CacheOutput("HealthCheck")
            .WithRequestTimeout(TimeSpan.FromSeconds(10))
            .DisableHttpMetrics();

        app.MapHealthChecks(AliveEndpoint, new() { Predicate = r => r.Tags.Contains("live") })
            .DisableHttpMetrics();

        app.MapHealthChecks(ReadyEndpoint, new() { Predicate = r => r.Tags.Contains("ready") })
            .DisableHttpMetrics();

        app.MapGet("/coffee", () => TypedResults.Text("I'm a teapot!", statusCode: StatusCodes.Status418ImATeapot))
            .ExcludeFromDescription();

        return app;
    }

    public static IHostApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        builder.Logging.ClearProviders();
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Filter.ByExcluding(logEvent =>
            {
                if (logEvent.Properties.TryGetValue("RequestPath", out var requestPath))
                {
                    return requestPath.ToString().StartsWith("\"/_");
                }
                if (logEvent.Properties.TryGetValue("Uri", out var uri))
                {
                    return uri.ToString().Contains("ingest/otlp");
                }
                return false;
            })
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]!;
                options.Protocol = builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"] == "grpc"
                    ? Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc
                    : Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf;
                var headers = builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]?.Split(',') ?? [];
                foreach (var header in headers)
                {
                    var (key, value) = header.Split('=') switch
                    {
                    [{ } k, { } v] => (k, v),
                        var v => throw new Exception($"Invalid header format {v}")
                    };

                    options.Headers.Add(key, value);
                }

                var serviceName = builder.Configuration["OTEL_SERVICE_NAME"];
                options.ResourceAttributes.Add("service.name", serviceName ?? builder.Environment.ApplicationName);
                options.ResourceAttributes.Add("deployment.environment", builder.Environment.EnvironmentName);

                var attributes = builder.Configuration["OTEL_LOGS_ATTRIBUTES"];
                if (string.IsNullOrWhiteSpace(attributes) is false)
                {
                    foreach (var attribute in attributes.Split(','))
                    {
                        var (key, value) = attribute.Split('=') switch
                        {
                        [{ } k, { } v] => (k, v),
                            var v => throw new Exception($"Invalid header format {v}")
                        };
                        options.ResourceAttributes.Add(key, value);
                    }
                }
            })
            .Enrich.FromLogContext());

        return builder;
    }

    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) => app.UseSerilogRequestLogging();

    private static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var protocol = builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"] == "grpc"
            ? OtlpExportProtocol.Grpc
            : OtlpExportProtocol.HttpProtobuf;

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = (ctx) => ctx.Request.Path.HasValue
                            && !ctx.Request.Path.StartsWithSegments(HealthEndpoint)
                            && !ctx.Request.Path.StartsWithSegments(AliveEndpoint)
                            && !ctx.Request.Path.StartsWithSegments(ReadyEndpoint);
                    })
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(endpoint) && endpoint.Contains("seq"))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"{endpoint}v1/traces");
                        options.Protocol = protocol;
                    });
                }
            });

        if (!string.IsNullOrWhiteSpace(endpoint) && !endpoint.Contains("seq"))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    private static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddOutputCache(
            configureOptions: static caching =>
                caching.AddPolicy("HealthCheck",
                build: static policy => policy.Expire(TimeSpan.FromSeconds(10))));

        var appStartup = new ServiceStartup();
        builder.Services.AddSingleton<IAppStartup>(appStartup);
        builder.Services.AddHealthChecks().AddCheck("ready", appStartup, tags: ["ready"]);

        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return builder;
    }
}
