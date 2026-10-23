using Condux.Otlp.Tests.Support;
using Xunit;

namespace Condux.Otlp.Tests;

/// <summary>
/// Decodes a traces payload that sets every field of every message, in both encodings.
/// </summary>
/// <remarks>
/// The traces counterpart of <see cref="EveryFieldTests"/>, for the same reason: a real exporter only
/// emits what it has, so the dropped counts, the trace state and the schema URLs would otherwise be read
/// by no test. The values differ from field to field so a decoder that wrote one field to the wrong
/// property cannot pass.
/// </remarks>
public class EveryFieldTraceTests
{
    private const string TraceId = "0af7651916cd43dd8448eb211c80319c";
    private const string SpanId = "b7ad6b7169203331";
    private const string ParentSpanId = "00f067aa0ba902b7";
    private const string LinkTraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
    private const string LinkSpanId = "53995c3f42cd8ad8";

    [Fact]
    public void ReadsEveryFieldFromProtobuf()
    {
        var result = OtlpTraces.ParseProtobuf(Payload());

        Assert.True(result.IsSuccess, result.Error);
        AssertEveryField(result.Value);
    }

    [Fact]
    public void ReadsEveryFieldFromJson()
    {
        var result = OtlpTraces.ParseJson($$$"""
            {"resourceSpans":[{
              "resource":{"attributes":[{"key":"host.name","value":{"stringValue":"web-01"}}],"droppedAttributesCount":7},
              "scopeSpans":[{
                "scope":{"name":"Checkout.Payments","version":"2.1.0"},
                "spans":[{
                  "traceId":"{{{TraceId}}}",
                  "spanId":"{{{SpanId}}}",
                  "traceState":"vendor=value",
                  "parentSpanId":"{{{ParentSpanId}}}",
                  "flags":769,
                  "name":"charge",
                  "kind":3,
                  "startTimeUnixNano":"1700000000000000001",
                  "endTimeUnixNano":"1700000000000000002",
                  "attributes":[{"key":"attempt","value":{"intValue":"42"}}],
                  "droppedAttributesCount":5,
                  "events":[{
                    "timeUnixNano":"1700000000000000003",
                    "name":"exception",
                    "attributes":[{"key":"exception.type","value":{"stringValue":"TimeoutError"}}],
                    "droppedAttributesCount":4
                  }],
                  "droppedEventsCount":6,
                  "links":[{
                    "traceId":"{{{LinkTraceId}}}",
                    "spanId":"{{{LinkSpanId}}}",
                    "traceState":"other=link",
                    "attributes":[{"key":"link.reason","value":{"stringValue":"retry"}}],
                    "droppedAttributesCount":9,
                    "flags":513
                  }],
                  "droppedLinksCount":8,
                  "status":{"message":"card_declined","code":2}
                }],
                "schemaUrl":"https://example.invalid/schema/scope"
              }],
              "schemaUrl":"https://example.invalid/schema/resource"
            }]}
            """);

        Assert.True(result.IsSuccess, result.Error);
        AssertEveryField(result.Value);
    }

    private static void AssertEveryField(ExportTraceServiceRequest request)
    {
        var resourceSpans = Assert.Single(request.ResourceSpans);
        Assert.Equal("https://example.invalid/schema/resource", resourceSpans.SchemaUrl);
        Assert.Equal(7u, resourceSpans.Resource?.DroppedAttributesCount);
        Assert.Equal("web-01", Assert.Single(resourceSpans.Resource!.Attributes).Value?.StringValue);

        var scopeSpans = Assert.Single(resourceSpans.ScopeSpans);
        Assert.Equal("https://example.invalid/schema/scope", scopeSpans.SchemaUrl);
        Assert.Equal("Checkout.Payments", scopeSpans.Scope?.Name);
        Assert.Equal("2.1.0", scopeSpans.Scope?.Version);

        var span = Assert.Single(scopeSpans.Spans);
        Assert.Equal(TraceId, Convert.ToHexString(span.TraceId).ToLowerInvariant());
        Assert.Equal(SpanId, Convert.ToHexString(span.SpanId).ToLowerInvariant());
        Assert.Equal("vendor=value", span.TraceState);
        Assert.Equal(ParentSpanId, Convert.ToHexString(span.ParentSpanId).ToLowerInvariant());
        Assert.Equal(769u, span.Flags);
        Assert.Equal(1, span.TraceFlags);
        Assert.Equal("charge", span.Name);
        Assert.Equal(SpanKind.Client, span.Kind);
        Assert.Equal(1700000000000000001UL, span.StartTimeUnixNano);
        Assert.Equal(1700000000000000002UL, span.EndTimeUnixNano);
        Assert.Equal(42L, Assert.Single(span.Attributes).Value?.IntValue);
        Assert.Equal(5u, span.DroppedAttributesCount);
        Assert.Equal(6u, span.DroppedEventsCount);
        Assert.Equal(8u, span.DroppedLinksCount);
        Assert.Equal(SpanStatusCode.Error, span.Status?.Code);
        Assert.Equal("card_declined", span.Status?.Message);

        var spanEvent = Assert.Single(span.Events);
        Assert.Equal(1700000000000000003UL, spanEvent.TimeUnixNano);
        Assert.Equal("exception", spanEvent.Name);
        Assert.Equal("TimeoutError", Assert.Single(spanEvent.Attributes).Value?.StringValue);
        Assert.Equal(4u, spanEvent.DroppedAttributesCount);

        var link = Assert.Single(span.Links);
        Assert.Equal(LinkTraceId, Convert.ToHexString(link.TraceId).ToLowerInvariant());
        Assert.Equal(LinkSpanId, Convert.ToHexString(link.SpanId).ToLowerInvariant());
        Assert.Equal("other=link", link.TraceState);
        Assert.Equal("retry", Assert.Single(link.Attributes).Value?.StringValue);
        Assert.Equal(9u, link.DroppedAttributesCount);
        Assert.Equal(513u, link.Flags);
        Assert.Equal(1, link.TraceFlags);
    }

    /// <summary>
    /// The same message as the JSON above, assembled from the field numbers and wire types the reference
    /// definitions in <c>proto-reference/</c> declare.
    /// </summary>
    private static byte[] Payload()
    {
        var resource = Wire.Concat(Wire.Bytes(1, StringAttribute("host.name", "web-01")), Wire.VarintField(2, 7));
        var scope = Wire.Concat(Wire.Bytes(1, "Checkout.Payments"u8.ToArray()), Wire.Bytes(2, "2.1.0"u8.ToArray()));

        var spanEvent = Wire.Concat(
            Wire.Fixed64Field(1, 1700000000000000003),
            Wire.Bytes(2, "exception"u8.ToArray()),
            Wire.Bytes(3, StringAttribute("exception.type", "TimeoutError")),
            Wire.VarintField(4, 4));

        var link = Wire.Concat(
            Wire.Bytes(1, Convert.FromHexString(LinkTraceId)),
            Wire.Bytes(2, Convert.FromHexString(LinkSpanId)),
            Wire.Bytes(3, "other=link"u8.ToArray()),
            Wire.Bytes(4, StringAttribute("link.reason", "retry")),
            Wire.VarintField(5, 9),
            Wire.Fixed32Field(6, 513));

        var status = Wire.Concat(Wire.Bytes(2, "card_declined"u8.ToArray()), Wire.VarintField(3, 2));

        var span = Wire.Concat(
            Wire.Bytes(1, Convert.FromHexString(TraceId)),
            Wire.Bytes(2, Convert.FromHexString(SpanId)),
            Wire.Bytes(3, "vendor=value"u8.ToArray()),
            Wire.Bytes(4, Convert.FromHexString(ParentSpanId)),
            Wire.Bytes(5, "charge"u8.ToArray()),
            Wire.VarintField(6, 3),
            Wire.Fixed64Field(7, 1700000000000000001),
            Wire.Fixed64Field(8, 1700000000000000002),
            Wire.Bytes(9, Wire.Concat(Wire.Bytes(1, "attempt"u8.ToArray()), Wire.Bytes(2, Wire.VarintField(3, 42)))),
            Wire.VarintField(10, 5),
            Wire.Bytes(11, spanEvent),
            Wire.VarintField(12, 6),
            Wire.Bytes(13, link),
            Wire.VarintField(14, 8),
            Wire.Bytes(15, status),
            Wire.Fixed32Field(16, 769));

        var scopeSpans = Wire.Concat(
            Wire.Bytes(1, scope),
            Wire.Bytes(2, span),
            Wire.Bytes(3, "https://example.invalid/schema/scope"u8.ToArray()));

        var resourceSpans = Wire.Concat(
            Wire.Bytes(1, resource),
            Wire.Bytes(2, scopeSpans),
            Wire.Bytes(3, "https://example.invalid/schema/resource"u8.ToArray()));

        return Wire.Bytes(1, resourceSpans);
    }

    private static byte[] StringAttribute(string key, string value)
        => Wire.Concat(Wire.Bytes(1, System.Text.Encoding.UTF8.GetBytes(key)), Wire.Bytes(2, Wire.Bytes(1, System.Text.Encoding.UTF8.GetBytes(value))));
}
