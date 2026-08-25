using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace Condux.Otlp.FixtureCapture;

/// <summary>
/// Emits one batch of log records through the real OpenTelemetry SDK, to every endpoint it is given.
/// </summary>
/// <remarks>
/// The batch is deliberately varied: a plain record, and one carrying a thrown exception with a real
/// stack from inside a trace, so the export exercises trace and span ids. That second record is what
/// makes the fixture able to catch a decoder that reads those ids as base64.
/// <para>
/// Both records leave in one export, which is what a real service does and what lets the fixture cover a
/// repeated field. Disposing the factory flushes the batch, so the export is complete when Emit returns.
/// </para>
/// </remarks>
internal static class LogEmitter
{
    internal static void Emit(IReadOnlyList<Uri> endpoints)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService(
                    serviceName: "checkout-api",
                    serviceVersion: "1.4.2"));
                options.IncludeFormattedMessage = true;
                foreach (var endpoint in endpoints)
                {
                    options.AddOtlpExporter(exporter =>
                    {
                        exporter.Endpoint = endpoint;
                        exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }
            });
        });

        var logger = loggerFactory.CreateLogger("Checkout.Payments");
        logger.LogInformation("Charging {Amount} {Currency} for order {OrderId}", 42.5, "EUR", "ord_7Q2");

        using var activity = Telemetry.Source.StartActivity("charge");
        logger.LogError(Failure(), "Payment provider rejected the charge for order {OrderId}", "ord_7Q2");
    }

    private static InvalidOperationException Failure()
    {
        try
        {
            throw new InvalidOperationException("card_declined: insufficient funds");
        }
        catch (InvalidOperationException caught)
        {
            return caught;
        }
    }
}
