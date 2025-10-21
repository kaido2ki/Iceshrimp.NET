using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Services;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Iceshrimp.Backend.Core.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        var cfg = builder.Configuration.GetRequiredSection("OpenTelemetry").Get<Config.OpenTelemetrySection>();
        if (!cfg?.Enabled == true) return;

        builder.Services.AddOpenTelemetry()
               .WithTracing(tracing => tracing
                                       .AddSource(TraceService.Source)
                                       .AddAspNetCoreInstrumentation(opts =>
                                       {
                                           opts.RecordException = true;
                                           opts.EnrichWithHttpRequest = (activity, req) =>
                                           {
                                               activity.AddTag("request.id", req.HttpContext.TraceIdentifier);
                                           };
                                       })
                                       .AddHttpClientInstrumentation(opts =>
                                       {
                                           opts.RecordException = true;
                                       })
                                       .AddNpgsql()
                                       .AddOtlpExporter())
               .WithMetrics(metrics => metrics
                                       .AddAspNetCoreInstrumentation()
                                       .AddHttpClientInstrumentation()
                                       .AddNpgsqlInstrumentation()
                                       .AddRuntimeInstrumentation()
                                       .AddOtlpExporter())
               .WithLogging(logging => logging
                                .AddOtlpExporter());
		

        builder.Logging.AddOpenTelemetry(opts=>
        {
            opts.SetResourceBuilder(ResourceBuilder.CreateDefault().AddEnvironmentVariableDetector()).AddOtlpExporter();
        });
    }
}
