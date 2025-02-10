using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Text.Json;
using System.Text;
using Serilog;

namespace Beseler.ServiceDefaults;

public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/_health", new HealthCheckOptions()
        {
            ResponseWriter = WriteResponse
        }).CacheOutput("HealthCheck").WithRequestTimeout(TimeSpan.FromSeconds(10));

        app.MapHealthChecks("/_alive", new HealthCheckOptions()
        {
            Predicate = r => r.Tags.Contains("live"),
            ResponseWriter = WriteResponse
        });

        app.MapHealthChecks("/_ready", new HealthCheckOptions()
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = WriteResponse
        });

        app.MapGet("/coffee", () => TypedResults.Text("I'm a teapot!", statusCode: StatusCodes.Status418ImATeapot))
            .ExcludeFromDescription();

        return app;
    }

    public static IHostApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {        
        builder.Logging.ClearProviders();
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Filter.ByExcluding(logEvent =>
                logEvent.Properties.TryGetValue("RequestPath", out var requestPath)
                    && requestPath.ToString() switch
                    {
                        { } path when path.StartsWith("\"/_health") => true,
                        { } path when path.StartsWith("\"/_alive") => true,
                        { } path when path.StartsWith("\"/_ready") => true,
                        _ => false
                    })
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]!;
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
                options.ResourceAttributes.Add("env", builder.Environment.EnvironmentName);

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
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        var useOtlpExporter = string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is false;
        if (useOtlpExporter)
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

        builder.Services.AddSingleton<StartupHealthCheck>();
        builder.Services.AddHealthChecks().AddCheck<StartupHealthCheck>("ready", tags: ["ready"]);
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        
        return builder;
    }

    private static Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonWriterOptions { Indented = true };

        using var memoryStream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("status", healthReport.Status.ToString());
            jsonWriter.WriteString("duration", healthReport.TotalDuration.ToString());
            jsonWriter.WriteStartObject("results");

            foreach (var healthReportEntry in healthReport.Entries)
            {
                jsonWriter.WriteStartObject(healthReportEntry.Key);
                jsonWriter.WriteString("status",
                    healthReportEntry.Value.Status.ToString());
                jsonWriter.WriteString("duration",
                    healthReportEntry.Value.Duration.ToString());
                jsonWriter.WriteString("description",
                    healthReportEntry.Value.Description);
                jsonWriter.WriteStartObject("data");

                foreach (var item in healthReportEntry.Value.Data)
                {
                    jsonWriter.WritePropertyName(item.Key);

                    JsonSerializer.Serialize(jsonWriter, item.Value,
                        item.Value?.GetType() ?? typeof(object));
                }

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        return context.Response.WriteAsync(
            Encoding.UTF8.GetString(memoryStream.ToArray()));
    }
}
