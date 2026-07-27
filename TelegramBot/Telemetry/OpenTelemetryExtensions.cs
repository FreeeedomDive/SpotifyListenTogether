using Core.Sessions;
using Core.Telemetry;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Configuration;
using Serilog.Filters;
using Serilog.Sinks.OpenTelemetry;

namespace TelegramBot.Telemetry;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddSltTelemetry(this IServiceCollection services)
    {
        if (!IsExportEnabled())
        {
            return services;
        }

        services
            .AddOpenTelemetry()
            .ConfigureResource(ConfigureResource)
            .WithTracing(tracing => tracing
                                    .AddHttpClientInstrumentation(ExcludeLongPolling)
                                    .AddSource(SltTelemetry.ActivitySourceName)
                                    .AddSource(NpgsqlSourceName)
                                    .AddOtlpExporter()
            )
            .WithMetrics(metrics => metrics
                                    .AddHttpClientInstrumentation()
                                    .AddRuntimeInstrumentation()
                                    .AddMeter(SltTelemetry.MeterName)
                                    .AddView(
                                        SltTelemetry.CommandDurationName,
                                        new ExplicitBucketHistogramConfiguration { Boundaries = DurationBuckets }
                                    )
                                    .AddOtlpExporter((_, reader) =>
                                        reader.TemporalityPreference = MetricReaderTemporalityPreference.Cumulative
                                    )
            );

        return services;
    }

    public static void StartSltTelemetry(this IServiceProvider services)
    {
        if (!IsExportEnabled())
        {
            return;
        }

        services.GetRequiredService<TracerProvider>();
        services.GetRequiredService<MeterProvider>();

        var sessionsService = services.GetRequiredService<ISessionsService>();
        SltTelemetry.TrackState(() => sessionsService.ActiveSessionsCount);
    }

    public static LoggerConfiguration WriteToOpenTelemetry(this LoggerSinkConfiguration sinkConfiguration)
    {
        return sinkConfiguration.Logger(logger => logger
                                                  .Filter.ByExcluding(Matching.FromSource(OpenTelemetrySourcePrefix))
                                                  .Filter.ByExcluding(Matching.FromSource(GrpcSourcePrefix))
                                                  .WriteTo.OpenTelemetry(ConfigureSink)
        );
    }

    public static bool IsExportEnabled()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EndpointVariable)))
        {
            return false;
        }

        var disabled = Environment.GetEnvironmentVariable(SdkDisabledVariable);
        return !string.Equals(disabled, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ServiceNameComesFromEnvironment()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ServiceNameVariable)))
        {
            return true;
        }

        var attributes = Environment.GetEnvironmentVariable(ResourceAttributesVariable);

        return attributes is not null
               && attributes.Contains($"{ServiceNameAttribute}=", StringComparison.OrdinalIgnoreCase);
    }

    private static void ConfigureSink(OpenTelemetrySinkOptions options)
    {
        options.IncludedData = IncludedData.TraceIdField
                               | IncludedData.SpanIdField
                               | IncludedData.SpecRequiredResourceAttributes
                               | IncludedData.MessageTemplateTextAttribute
                               | IncludedData.SourceContextAttribute;

        if (!ServiceNameComesFromEnvironment())
        {
            options.ResourceAttributes[ServiceNameAttribute] = FallbackServiceName;
        }
    }

    private static void ConfigureResource(ResourceBuilder resource)
    {
        if (!ServiceNameComesFromEnvironment())
        {
            resource.AddService(FallbackServiceName);
        }
    }

    private static void ExcludeLongPolling(HttpClientTraceInstrumentationOptions options)
    {
        options.FilterHttpRequestMessage = request =>
            request.RequestUri?.AbsolutePath.EndsWith(LongPollingMethod, StringComparison.OrdinalIgnoreCase) != true;
    }

    private const string EndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string SdkDisabledVariable = "OTEL_SDK_DISABLED";
    private const string ServiceNameVariable = "OTEL_SERVICE_NAME";
    private const string ResourceAttributesVariable = "OTEL_RESOURCE_ATTRIBUTES";

    private const string ServiceNameAttribute = "service.name";
    private const string FallbackServiceName = "spotify-listen-together-bot";

    private const string NpgsqlSourceName = "Npgsql";

    private const string LongPollingMethod = "/getUpdates";

    private const string OpenTelemetrySourcePrefix = "OpenTelemetry";
    private const string GrpcSourcePrefix = "Grpc";

    private static readonly double[] DurationBuckets =
    [
        0.01, 0.025, 0.05, 0.075, 0.1, 0.15, 0.2, 0.25, 0.35, 0.5,
        0.75, 1, 1.25, 1.5, 2, 2.5, 5, 7.5, 10,
    ];

    private static readonly double[] ParticipantsBuckets =
        [1, 2, 3, 4, 5, 6, 8, 10, 15, 20];
}
