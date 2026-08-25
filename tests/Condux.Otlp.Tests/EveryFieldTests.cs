using Condux.Otlp.Tests.Support;
using Xunit;

namespace Condux.Otlp.Tests;

/// <summary>
/// Decodes a payload that sets every field of every message, in both encodings.
/// </summary>
/// <remarks>
/// The captured fixtures cannot do this job. A real exporter emits only what it has, so a field it
/// leaves at its default is read by no test at all, and a decoder that names the wrong wire type for one
/// of those makes it silently empty for ever while every other test stays green. Three fields were in
/// exactly that position before this file existed: the dropped-attribute counts, the scope version, and
/// both schema URLs.
/// <para>
/// The values differ from field to field on purpose. Identical values would let a decoder that wrote a
/// field to the wrong property pass.
/// </para>
/// </remarks>
public class EveryFieldTests
{
    [Fact]
    public void ReadsEveryFieldFromProtobuf()
    {
        var result = OtlpLogs.ParseProtobuf(Payload());

        Assert.True(result.IsSuccess, result.Error);
        AssertEveryField(result.Value);
    }

    [Fact]
    public void ReadsEveryFieldFromJson()
    {
        var result = OtlpLogs.ParseJson("""
            {"resourceLogs":[{
              "resource":{
                "attributes":[{"key":"host.name","value":{"stringValue":"web-01"}}],
                "droppedAttributesCount":7
              },
              "scopeLogs":[{
                "scope":{
                  "name":"Checkout.Payments",
                  "version":"2.1.0",
                  "attributes":[{"key":"scope.tag","value":{"boolValue":true}}],
                  "droppedAttributesCount":3
                },
                "logRecords":[{
                  "timeUnixNano":"1700000000000000001",
                  "observedTimeUnixNano":"1700000000000000002",
                  "severityNumber":17,
                  "severityText":"Error",
                  "body":{"stringValue":"the charge was declined"},
                  "attributes":[{"key":"attempt","value":{"intValue":"42"}}],
                  "droppedAttributesCount":5,
                  "flags":257,
                  "traceId":"0af7651916cd43dd8448eb211c80319c",
                  "spanId":"b7ad6b7169203331",
                  "eventName":"checkout.charge"
                }],
                "schemaUrl":"https://example.invalid/schema/scope"
              }],
              "schemaUrl":"https://example.invalid/schema/resource"
            }]}
            """);

        Assert.True(result.IsSuccess, result.Error);
        AssertEveryField(result.Value);
    }

    /// <summary>
    /// An attribute of every kind a value can take, over protobuf. The JSON side has had this since it
    /// was written; the protobuf side had not, and a mutation sweep found that a nested map or a byte
    /// array would have decoded as nothing without a single test noticing. Neither kind appears in any
    /// captured fixture, because none of the three producers emitted one.
    /// </summary>
    [Fact]
    public void ReadsEveryValueKindFromProtobuf()
    {
        var record = Wire.Concat(
            Attribute("text", Wire.Bytes(1, "hello"u8.ToArray())),
            Attribute("flag", Wire.VarintField(2, 1)),
            Attribute("count", Wire.VarintField(3, 42)),
            Attribute("ratio", Wire.Fixed64Field(4, BitConverter.DoubleToUInt64Bits(0.25))),
            Attribute("list", Wire.Bytes(5, Wire.Bytes(1, Wire.Bytes(1, "a"u8.ToArray())))),
            Attribute("map", Wire.Bytes(6, Wire.Bytes(1, Wire.Concat(
                Wire.Bytes(1, "inner"u8.ToArray()),
                Wire.Bytes(2, Wire.VarintField(2, 1)))))),
            Attribute("raw", Wire.Bytes(7, [1, 2, 3])));

        var result = OtlpLogs.ParseProtobuf(SingleRecord.Wrap(record));

        Assert.True(result.IsSuccess, result.Error);
        var decoded = SingleRecord.From(result.Value);
        Assert.Equal("hello", SingleRecord.Attribute(decoded, "text").StringValue);
        Assert.True(SingleRecord.Attribute(decoded, "flag").BoolValue);
        Assert.Equal(42L, SingleRecord.Attribute(decoded, "count").IntValue);
        Assert.Equal(0.25, SingleRecord.Attribute(decoded, "ratio").DoubleValue);
        Assert.Equal("a", SingleRecord.Attribute(decoded, "list").ArrayValue!.Values[0].StringValue);
        Assert.Equal("inner", SingleRecord.Attribute(decoded, "map").KvlistValue!.Values[0].Key);
        Assert.True(SingleRecord.Attribute(decoded, "map").KvlistValue!.Values[0].Value!.BoolValue);
        Assert.Equal<byte[]>([1, 2, 3], SingleRecord.Attribute(decoded, "raw").BytesValue!);

        // The kind decides which member a caller may read, so it is asserted alongside every value.
        Assert.Equal(AnyValueKind.Kvlist, SingleRecord.Attribute(decoded, "map").Kind);
        Assert.Equal(AnyValueKind.Bytes, SingleRecord.Attribute(decoded, "raw").Kind);
    }

    /// <summary>One log-record attribute carrying the value bytes given.</summary>
    private static byte[] Attribute(string key, byte[] value)
    {
        const int AttributesField = 6;
        return Wire.Bytes(AttributesField, Wire.Concat(
            Wire.Bytes(1, System.Text.Encoding.UTF8.GetBytes(key)),
            Wire.Bytes(2, value)));
    }

    private static void AssertEveryField(ExportLogsServiceRequest request)
    {
        var resourceLogs = Assert.Single(request.ResourceLogs);
        Assert.Equal("https://example.invalid/schema/resource", resourceLogs.SchemaUrl);

        var resource = resourceLogs.Resource;
        Assert.NotNull(resource);
        Assert.Equal(7u, resource.DroppedAttributesCount);
        Assert.Equal("host.name", Assert.Single(resource.Attributes).Key);
        Assert.Equal("web-01", resource.Attributes[0].Value?.StringValue);

        var scopeLogs = Assert.Single(resourceLogs.ScopeLogs);
        Assert.Equal("https://example.invalid/schema/scope", scopeLogs.SchemaUrl);

        var scope = scopeLogs.Scope;
        Assert.NotNull(scope);
        Assert.Equal("Checkout.Payments", scope.Name);
        Assert.Equal("2.1.0", scope.Version);
        Assert.Equal(3u, scope.DroppedAttributesCount);
        Assert.Equal("scope.tag", Assert.Single(scope.Attributes).Key);
        Assert.True(scope.Attributes[0].Value?.BoolValue);

        var record = Assert.Single(scopeLogs.LogRecords);
        Assert.Equal(1700000000000000001UL, record.TimeUnixNano);
        Assert.Equal(1700000000000000002UL, record.ObservedTimeUnixNano);
        Assert.Equal(SeverityNumber.Error, record.SeverityNumber);
        Assert.Equal("Error", record.SeverityText);
        Assert.Equal("the charge was declined", record.Body?.StringValue);
        Assert.Equal("attempt", Assert.Single(record.Attributes).Key);
        Assert.Equal(42L, record.Attributes[0].Value?.IntValue);
        Assert.Equal(5u, record.DroppedAttributesCount);
        Assert.Equal(257u, record.Flags);
        Assert.Equal(1, record.TraceFlags);
        Assert.Equal("0AF7651916CD43DD8448EB211C80319C", Convert.ToHexString(record.TraceId));
        Assert.Equal("B7AD6B7169203331", Convert.ToHexString(record.SpanId));
        Assert.Equal("checkout.charge", record.EventName);
    }

    /// <summary>
    /// The same message as the JSON above, assembled from the field numbers and wire types the reference
    /// definitions in <c>proto-reference/</c> declare.
    /// </summary>
    private static byte[] Payload()
    {
        var resource = Wire.Concat(
            Wire.Bytes(1, StringAttribute("host.name", "web-01")),
            Wire.VarintField(2, 7));

        var scope = Wire.Concat(
            Wire.Bytes(1, "Checkout.Payments"u8.ToArray()),
            Wire.Bytes(2, "2.1.0"u8.ToArray()),
            Wire.Bytes(3, Wire.Concat(Wire.Bytes(1, "scope.tag"u8.ToArray()), Wire.Bytes(2, Wire.VarintField(2, 1)))),
            Wire.VarintField(4, 3));

        var record = Wire.Concat(
            Wire.Fixed64Field(1, 1700000000000000001),
            Wire.VarintField(2, 17),
            Wire.Bytes(3, "Error"u8.ToArray()),
            Wire.Bytes(5, Wire.Bytes(1, "the charge was declined"u8.ToArray())),
            Wire.Bytes(6, Wire.Concat(Wire.Bytes(1, "attempt"u8.ToArray()), Wire.Bytes(2, Wire.VarintField(3, 42)))),
            Wire.VarintField(7, 5),
            Wire.Fixed32Field(8, 257),
            Wire.Bytes(9, Convert.FromHexString("0af7651916cd43dd8448eb211c80319c")),
            Wire.Bytes(10, Convert.FromHexString("b7ad6b7169203331")),
            Wire.Fixed64Field(11, 1700000000000000002),
            Wire.Bytes(12, "checkout.charge"u8.ToArray()));

        var scopeLogs = Wire.Concat(
            Wire.Bytes(1, scope),
            Wire.Bytes(2, record),
            Wire.Bytes(3, "https://example.invalid/schema/scope"u8.ToArray()));

        var resourceLogs = Wire.Concat(
            Wire.Bytes(1, resource),
            Wire.Bytes(2, scopeLogs),
            Wire.Bytes(3, "https://example.invalid/schema/resource"u8.ToArray()));

        return Wire.Bytes(1, resourceLogs);
    }

    private static byte[] StringAttribute(string key, string value)
        => Wire.Concat(Wire.Bytes(1, System.Text.Encoding.UTF8.GetBytes(key)), Wire.Bytes(2, Wire.Bytes(1, System.Text.Encoding.UTF8.GetBytes(value))));
}
