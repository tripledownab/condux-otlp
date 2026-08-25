using Condux.Otlp.Tests.Support;
using Xunit;

namespace Condux.Otlp.Tests;

/// <summary>
/// Decodes exports captured from real OpenTelemetry software. These are the tests that can disprove an
/// assumption, because nothing in this repository produced the bytes they read.
/// </summary>
public class RealExporterTests
{
    [Theory]
    [InlineData(Fixture.DotnetSdkProtobuf)]
    [InlineData(Fixture.CollectorProtobuf)]
    public void DecodesAnExportFromARealProtobufExporter(string fixture)
    {
        var result = OtlpLogs.ParseProtobuf(Fixture.Bytes(fixture));

        Assert.True(result.IsSuccess, result.Error);
        var scope = Assert.Single(Assert.Single(result.Value.ResourceLogs).ScopeLogs);
        Assert.Equal("Checkout.Payments", scope.Scope?.Name);
        Assert.Equal(2, scope.LogRecords.Count);
    }

    /// <summary>
    /// Both JSON fixtures, from two implementations that share no code. The decoder reads member names
    /// as lowerCamelCase only and enum values as integers only, which is what the encoding requires but
    /// would be a costly guess if only one producer had ever been tried: a stricter reading than reality
    /// rejects a whole export over a field it could have read.
    /// </summary>
    [Theory]
    [InlineData(Fixture.CollectorJson)]
    [InlineData(Fixture.JsSdkJson)]
    public void DecodesAnExportFromARealJsonExporter(string fixture)
    {
        var result = OtlpLogs.ParseJson(Fixture.Bytes(fixture));

        Assert.True(result.IsSuccess, result.Error);
        var scope = Assert.Single(Assert.Single(result.Value.ResourceLogs).ScopeLogs);
        Assert.Equal(2, scope.LogRecords.Count);
        Assert.Equal(SeverityNumber.Info, scope.LogRecords[0].SeverityNumber);
        Assert.Equal(SeverityNumber.Error, scope.LogRecords[1].SeverityNumber);
    }

    /// <summary>
    /// Hex ids confirmed by the second implementation as well. One producer agreeing with a decoder can
    /// mean both are wrong the same way; two independent ones agreeing cannot.
    /// </summary>
    [Fact]
    public void ReadsHexTraceIdsFromASecondImplementationToo()
    {
        var result = OtlpLogs.ParseJson(Fixture.Bytes(Fixture.JsSdkJson));

        Assert.True(result.IsSuccess, result.Error);
        var record = result.Value.ResourceLogs[0].ScopeLogs[0].LogRecords[1];
        Assert.Equal(16, record.TraceId.Length);
        Assert.Equal(8, record.SpanId.Length);
        Assert.Equal(1, record.TraceFlags);
    }

    /// <summary>
    /// The two collector fixtures are one export in both encodings, so decoding them must produce the
    /// same message. This is the test that pins the encodings' one disagreement: JSON writes trace and
    /// span ids as hex where the standard protobuf JSON mapping would use base64, and a decoder that
    /// takes the standard route produces different bytes here while every other field still matches.
    /// </summary>
    [Fact]
    public void BothEncodingsOfOneExportDecodeToTheSameMessage()
    {
        var fromProtobuf = OtlpLogs.ParseProtobuf(Fixture.Bytes(Fixture.CollectorProtobuf));
        var fromJson = OtlpLogs.ParseJson(Fixture.Bytes(Fixture.CollectorJson));

        Assert.True(fromProtobuf.IsSuccess, fromProtobuf.Error);
        Assert.True(fromJson.IsSuccess, fromJson.Error);
        Assert.Equal(MessageDump.Render(fromProtobuf.Value), MessageDump.Render(fromJson.Value));
    }

    [Fact]
    public void KeepsTheResourceAttributesTheExporterSet()
    {
        var result = OtlpLogs.ParseProtobuf(Fixture.Bytes(Fixture.CollectorProtobuf));

        Assert.True(result.IsSuccess, result.Error);
        var resource = Assert.Single(result.Value.ResourceLogs).Resource;
        Assert.NotNull(resource);
        Assert.Equal("checkout-api", Attribute(resource.Attributes, "service.name"));
        Assert.Equal("1.4.2", Attribute(resource.Attributes, "service.version"));
    }

    [Fact]
    public void KeepsTheSeverityScaleTheExporterUsed()
    {
        var records = Records(Fixture.CollectorProtobuf);

        Assert.Equal(SeverityNumber.Info, records[0].SeverityNumber);
        Assert.Equal(SeverityNumber.Error, records[1].SeverityNumber);
        Assert.True(records[1].SeverityNumber >= SeverityNumber.Error);
    }

    [Fact]
    public void KeepsTheExceptionAttributesTheLoggingBridgeAdded()
    {
        var record = Records(Fixture.CollectorProtobuf)[1];

        Assert.Equal("InvalidOperationException", Attribute(record.Attributes, "exception.type"));
        Assert.Contains("card_declined", Attribute(record.Attributes, "exception.message"));
        Assert.Contains("LogEmitter.Failure()", Attribute(record.Attributes, "exception.stacktrace"));
    }

    [Fact]
    public void ReadsTraceAndSpanIdsAtTheirProtocolLengths()
    {
        var record = Records(Fixture.CollectorProtobuf)[1];

        Assert.Equal(16, record.TraceId.Length);
        Assert.Equal(8, record.SpanId.Length);
        Assert.Equal(1, record.TraceFlags);
    }

    /// <summary>
    /// A record with no span carries no ids at all, which is different from carrying zeroed ones and is
    /// how a reader tells that a log line belongs to no trace.
    /// </summary>
    [Fact]
    public void LeavesTraceAndSpanIdsEmptyOnARecordOutsideATrace()
    {
        var record = Records(Fixture.CollectorProtobuf)[0];

        Assert.Empty(record.TraceId);
        Assert.Empty(record.SpanId);
    }

    [Fact]
    public void ReadsTheBodyAndTheTimestampsTheExporterSet()
    {
        var record = Records(Fixture.CollectorProtobuf)[0];

        Assert.Equal("Charging 42.5 EUR for order ord_7Q2", record.Body?.StringValue);
        Assert.True(record.TimeUnixNano > 0);
        Assert.True(record.ObservedTimeUnixNano > 0);
    }

    /// <summary>
    /// The exporter writes a double attribute as a JSON number and as a protobuf fixed64. Both must land
    /// on the same value, which is the narrow case a reinterpretation bug would break.
    /// </summary>
    [Fact]
    public void ReadsADoubleAttributeIdenticallyFromBothEncodings()
    {
        var fromProtobuf = Records(Fixture.CollectorProtobuf)[0];
        var fromJson = OtlpLogs.ParseJson(Fixture.Bytes(Fixture.CollectorJson))
            .Value!.ResourceLogs[0].ScopeLogs[0].LogRecords[0];

        var amount = fromProtobuf.Attributes.Single(pair => pair.Key == "Amount").Value;
        Assert.Equal(AnyValueKind.Double, amount?.Kind);
        Assert.Equal(42.5, amount!.DoubleValue);
        Assert.Equal(42.5, fromJson.Attributes.Single(pair => pair.Key == "Amount").Value!.DoubleValue);
    }

    private static List<LogRecord> Records(string fixture)
    {
        var result = OtlpLogs.ParseProtobuf(Fixture.Bytes(fixture));
        Assert.True(result.IsSuccess, result.Error);
        return result.Value.ResourceLogs[0].ScopeLogs[0].LogRecords;
    }

    private static string Attribute(List<KeyValue> attributes, string key)
        => attributes.Single(pair => pair.Key == key).Value!.StringValue!;
}
