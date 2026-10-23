using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Condux.Otlp.FixtureCapture;

/// <summary>
/// Emits one batch of spans through the real OpenTelemetry SDK, to every endpoint it is given.
/// </summary>
/// <remarks>
/// The batch is a server span with a client span under it, so the export carries a parent span id. The
/// child is the varied one: it records an event, an exception with a real stack, a link to a span in
/// another trace, and an error status with a description, while the parent ends with an ok status. That
/// covers both status outcomes, both kinds of nested message, and every id field the JSON encoding
/// writes as hex.
/// <para>
/// Disposing the provider flushes the batch, so the export is complete when Emit returns.
/// </para>
/// </remarks>
internal static class TraceEmitter
{
    internal static void Emit(IReadOnlyList<Uri> endpoints)
    {
        var builder = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService(serviceName: "checkout-api", serviceVersion: "1.4.2"))
            .AddSource(Telemetry.Source.Name);
        foreach (var endpoint in endpoints)
        {
            builder.AddOtlpExporter(exporter =>
            {
                exporter.Endpoint = endpoint;
                exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        }

        using var provider = builder.Build();

        using var request = Telemetry.Source.StartActivity("POST /checkout", ActivityKind.Server);
        request?.SetTag("http.request.method", "POST");
        request?.SetTag("http.response.status_code", 502);

        var retried = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        var link = new ActivityLink(retried, new ActivityTagsCollection { ["link.reason"] = "retry of an earlier attempt" });
        using (var charge = Telemetry.Source.StartActivity("charge", ActivityKind.Client, request?.Context ?? default, links: [link]))
        {
            charge?.SetTag("payment.provider", "acme-pay");
            charge?.SetTag("payment.amount", 42.5);
            charge?.AddEvent(new ActivityEvent("provider.request.sent"));
            charge?.AddException(Failure());
            charge?.SetStatus(ActivityStatusCode.Error, "card_declined");
        }

        request?.SetStatus(ActivityStatusCode.Ok);
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
