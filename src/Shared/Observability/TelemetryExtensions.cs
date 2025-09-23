using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Observability;

public static class TelemetryExtensions
{
    public static IServiceCollection AddAppTelemetry(this IServiceCollection services, string serviceName)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName);

        services.AddOpenTelemetry()
            .WithTracing(tracer => tracer
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(opt =>
                {
                    opt.Endpoint = new Uri("http://aspire-dashboard:18889");
                }))
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(opt =>
                {
                    opt.Endpoint = new Uri("http://aspire-dashboard:18889");
                }));

        return services;
    }
}
