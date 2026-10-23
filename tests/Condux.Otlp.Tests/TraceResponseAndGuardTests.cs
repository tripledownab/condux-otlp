using Condux.Otlp.Tests.Support;
using Xunit;

namespace Condux.Otlp.Tests;

/// <summary>
/// The traces response, and the traces decoders' handling of payloads a conformant exporter would not
/// send. The logs counterparts are <see cref="ResponseTests"/> and <see cref="ProtobufGuardTests"/>.
/// </summary>
public class TraceResponseAndGuardTests
{
    [Fact]
    public void EncodesAFullSuccessAsAnEmptyMessage()
    {
        var response = new ExportTraceServiceResponse();

        Assert.Empty(response.ToProtobuf());
        Assert.Equal("{}", response.ToJson());
    }

    /// <summary>
    /// The one place a traces response differs from a logs response: the JSON name of the count.
    /// </summary>
    [Fact]
    public void NamesTheRejectedCountForSpansInJson()
    {
        var response = new ExportTraceServiceResponse
        {
            PartialSuccess = new ExportTracePartialSuccess { RejectedSpans = 3, ErrorMessage = "over quota" },
        };

        Assert.Equal("""{"partialSuccess":{"rejectedSpans":"3","errorMessage":"over quota"}}""", response.ToJson());
    }

    [Fact]
    public void EncodesARejectionCountAndMessageAsProtobuf()
    {
        var response = new ExportTraceServiceResponse
        {
            PartialSuccess = new ExportTracePartialSuccess { RejectedSpans = 3, ErrorMessage = "over quota" },
        };

        var expected = Wire.Bytes(1, Wire.Concat(Wire.VarintField(1, 3), Wire.Bytes(2, "over quota"u8.ToArray())));
        Assert.Equal(expected, response.ToProtobuf());
    }

    /// <summary>A field the protocol may add later is stepped over, so an older reader keeps working.</summary>
    [Fact]
    public void StepsOverASpanFieldItDoesNotKnow()
    {
        var span = Wire.Concat(Wire.Bytes(5, "charge"u8.ToArray()), Wire.VarintField(99, 1));

        var result = OtlpTraces.ParseProtobuf(Wrap(span));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("charge", Single(result.Value).Name);
    }

    [Fact]
    public void RejectsASpanWhoseLengthRunsPastThePayload()
    {
        var truncated = Wire.BytesWithLength(5, 50, "charge"u8.ToArray());

        Assert.False(OtlpTraces.ParseProtobuf(Wrap(truncated)).IsSuccess);
    }

    /// <summary>
    /// The JSON encoding writes ids as hex. A base64 id, which the general protobuf JSON mapping would
    /// produce, is refused rather than decoded to the wrong bytes.
    /// </summary>
    [Fact]
    public void RefusesABase64ParentSpanIdInJson()
    {
        var result = OtlpTraces.ParseJson("""
            {"resourceSpans":[{"scopeSpans":[{"spans":[{"name":"charge","parentSpanId":"APBnqguqArc="}]}]}]}
            """);

        Assert.False(result.IsSuccess);
    }

    /// <summary>The encoding requires an enum as its integer value, so a name is refused.</summary>
    [Fact]
    public void RefusesAStatusCodeWrittenAsItsNameInJson()
    {
        var result = OtlpTraces.ParseJson("""
            {"resourceSpans":[{"scopeSpans":[{"spans":[{"name":"charge","status":{"code":"STATUS_CODE_ERROR"}}]}]}]}
            """);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void RefusesASpanKindWrittenAsItsNameInJson()
    {
        var result = OtlpTraces.ParseJson("""
            {"resourceSpans":[{"scopeSpans":[{"spans":[{"name":"charge","kind":"SPAN_KIND_SERVER"}]}]}]}
            """);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ReportsAPayloadThatIsNotATracesExport()
    {
        var result = OtlpTraces.ParseJson("""["not","an","export"]""");

        Assert.False(result.IsSuccess);
        Assert.Contains("traces", result.Error);
    }

    private static byte[] Wrap(byte[] span)
        => Wire.Bytes(1, Wire.Bytes(2, Wire.Bytes(2, span)));

    private static Span Single(ExportTraceServiceRequest request)
        => Assert.Single(Assert.Single(Assert.Single(request.ResourceSpans).ScopeSpans).Spans);
}
