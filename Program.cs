using System.Diagnostics;
using System.Net.Http;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;


namespace GettingStartedJaeger;

public class Program
{
    private static readonly ActivitySource MyActivitySource = new("ProccessName");

    public static async Task Main()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: "ServiceName", serviceVersion: "1.0.0"))
            .AddSource("ProccessName")
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317");
            })
            .Build();

        using (var client = new HttpClient())
        {
            using (var activity = MyActivitySource.StartActivity("ActivityName"))
            {
                if (activity != null)
                {
                    activity.SetTag(
                        Environment.GetEnvironmentVariable("OTEL_ACTIVITY_TAG_KEY") ?? "Test Tag", //The key of the tag
                        Environment.GetEnvironmentVariable("OTEL_ACTIVITY_TAG_VALUE") ?? "OK" // The value of the tag
                    );
                    activity.AddEvent(new ActivityEvent("This is event log value 1"));

                    // Execute the HTTP call test
                    var response = await client.GetStringAsync(Environment.GetEnvironmentVariable("OTEL_ACTIVITY_URL_VALUE") ?? "http://httpbin.org/get");

                    logger.LogInformation("This is log test");
                    activity.AddEvent(new ActivityEvent("This is event log value 2"));
                }
                else
                {
                    logger.LogWarning("Failed to start activity for 'ActivityName'");
                }
            }
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Configure logging
        services.AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information);
    }
}
