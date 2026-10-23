using Condux.Otlp.Tests.Support;
using Xunit;

namespace Condux.Otlp.Tests;

/// <summary>
/// Decodes traces exports captured from real OpenTelemetry software: the .NET SDK, the collector in both
/// encodings, and the JS SDK. Each export is a server span with a failed client span under it, the child
/// carrying an event, an exception, a link to another trace and an error status.
/// </summary>
public class RealExporterTraceTests
{
    [Theory]
    [InlineData(Fixture.DotnetSdkTracesProtobuf)]
    [InlineData(Fixture.CollectorTracesProtobuf)]
    public void DecodesATracesExportFromARealProtobufExporter(string fixture)
        => AssertTheCapturedBatch(OtlpTraces.ParseProtobuf(Fixture.Bytes(fixture)));

    /// <summary>
    /// Both JSON fixtures, from two implementations that share no code, so the decoder's strict reading of
    /// member names, hex ids and integer enums is checked against more than one producer's habit.
    /// </summary>
    [Theory]
    [InlineData(Fixture.CollectorTracesJson)]
    [InlineData(Fixture.JsSdkTracesJson)]
    public void DecodesATracesExportFromARealJsonExporter(string fixture)
        => AssertTheCapturedBatch(OtlpTraces.ParseJson(Fixture.Bytes(fixture)));

    /// <summary>
    /// The two collector fixtures are one export in both encodings. A span carries more hex ids than a
    /// log record, the parent span id and a link's two ids among them, and a decoder that read any of them
    /// as base64 would produce different bytes here while every other field still matched.
    /// </summary>
    [Fact]
    public void BothEncodingsOfOneTracesExportDecodeToTheSameMessage()
    {
        var fromProtobuf = OtlpTraces.ParseProtobuf(Fixture.Bytes(Fixture.CollectorTracesProtobuf));
        var fromJson = OtlpTraces.ParseJson(Fixture.Bytes(Fixture.CollectorTracesJson));

        Assert.True(fromProtobuf.IsSuccess, fromProtobuf.Error);
        Assert.True(fromJson.IsSuccess, fromJson.Error);
        Assert.Equal(MessageDump.Render(fromProtobuf.Value), MessageDump.Render(fromJson.Value));
    }

    [Fact]
    public void KeepsTheResourceAttributesTheExporterSet()
    {
        var result = OtlpTraces.ParseProtobuf(Fixture.Bytes(Fixture.CollectorTracesProtobuf));

        Assert.True(result.IsSuccess, result.Error);
        var resource = Assert.Single(result.Value.ResourceSpans).Resource;
        Assert.NotNull(resource);
        Assert.Equal("checkout-api", resource.Attributes.Single(pair => pair.Key == "service.name").Value!.StringValue);
    }

    private static void AssertTheCapturedBatch(OtlpReadResult<ExportTraceServiceRequest> result)
    {
        Assert.True(result.IsSuccess, result.Error);
        var spans = Assert.Single(Assert.Single(result.Value.ResourceSpans).ScopeSpans).Spans;
        Assert.Equal(2, spans.Count);

        // Exporters write a span when it ends, so the child, which ends first, may come first. Find each
        // by name rather than depending on that order.
        var request = spans.Single(span => span.Name == "POST /checkout");
        var charge = spans.Single(span => span.Name == "charge");

        Assert.Equal(SpanKind.Server, request.Kind);
        Assert.Equal(SpanKind.Client, charge.Kind);
        Assert.Equal(SpanStatusCode.Ok, request.Status?.Code);
        Assert.Equal(SpanStatusCode.Error, charge.Status?.Code);
        Assert.Equal("card_declined", charge.Status?.Message);

        Assert.Equal(16, charge.TraceId.Length);
        Assert.Equal(8, charge.SpanId.Length);
        Assert.Equal(request.TraceId, charge.TraceId);
        Assert.Equal(request.SpanId, charge.ParentSpanId);
        Assert.Empty(request.ParentSpanId);
        Assert.True(charge.EndTimeUnixNano >= charge.StartTimeUnixNano);
        Assert.True(charge.StartTimeUnixNano > 0);

        Assert.Equal("acme-pay", charge.Attributes.Single(pair => pair.Key == "payment.provider").Value!.StringValue);
        Assert.Equal(42.5, charge.Attributes.Single(pair => pair.Key == "payment.amount").Value!.DoubleValue);

        Assert.Contains(charge.Events, spanEvent => spanEvent.Name == "provider.request.sent");
        var exception = charge.Events.Single(spanEvent => spanEvent.Name == "exception");
        Assert.Contains("card_declined", exception.Attributes.Single(pair => pair.Key == "exception.message").Value!.StringValue);
        Assert.Contains("/capture", exception.Attributes.Single(pair => pair.Key == "exception.stacktrace").Value!.StringValue);

        var link = Assert.Single(charge.Links);
        Assert.Equal(16, link.TraceId.Length);
        Assert.Equal(8, link.SpanId.Length);
        Assert.NotEqual(charge.TraceId, link.TraceId);
        Assert.Equal("retry of an earlier attempt", link.Attributes.Single(pair => pair.Key == "link.reason").Value!.StringValue);
    }
}
