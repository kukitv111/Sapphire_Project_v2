using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace Sapphire.Shared.Security.Telemetry;

public static class SapphireTelemetry
{
    private static readonly Meter Meter = new("Sapphire.Http");
    private static readonly Counter<long> HttpFailures = Meter.CreateCounter<long>("sapphire_http_5xx_total");
    private static readonly Counter<long> PoolExhaustions = Meter.CreateCounter<long>("sapphire_db_pool_exhaustion_total");

    public static IServiceCollection AddSapphireTelemetry(this IServiceCollection services,
        IConfiguration configuration, string serviceName)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationHeaderHandler>();
        services.ConfigureHttpClientDefaults(b => b.AddHttpMessageHandler<CorrelationHeaderHandler>());
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation()
                 .AddHttpClientInstrumentation();
                if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                    t.AddOtlpExporter();
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation()
                 .AddHttpClientInstrumentation()
                 .AddMeter("Sapphire.Http", "Sapphire.Messaging");
                if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                    m.AddOtlpExporter();
            });
        return services;
    }

    public static IApplicationBuilder UseSapphireCorrelation(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var incoming = context.Request.Headers["X-Correlation-ID"].ToString();
            var correlationId = Guid.TryParse(incoming, out var parsed)
                ? parsed.ToString("N")
                : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            using (context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Sapphire.Request").BeginScope(new Dictionary<string, object>
                { ["CorrelationId"] = correlationId }))
            {
                context.Items["CorrelationId"] = correlationId;
                try
                {
                    await next(context);
                    if (context.Response.StatusCode >= 500)
                        HttpFailures.Add(1);
                }
                catch (Exception)
                {
                    HttpFailures.Add(1);
                    throw;
                }
            }
        });

    public static IApplicationBuilder UseSapphirePoolDiagnostics(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            try { await next(context); }
            catch (Exception ex)
            {
                var detail = ex.ToString();
                if (detail.Contains("pool has been exhausted", StringComparison.OrdinalIgnoreCase) ||
                    detail.Contains("waiting to obtain a connection from the pool", StringComparison.OrdinalIgnoreCase))
                    PoolExhaustions.Add(1);
                throw;
            }
        });
}

public sealed class CorrelationHeaderHandler(IHttpContextAccessor context) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var id = context.HttpContext?.Items["CorrelationId"]?.ToString()
            ?? Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(id)) request.Headers.TryAddWithoutValidation("X-Correlation-ID", id);
        return base.SendAsync(request, ct);
    }
}
