using Condux.Otlp.Tests.Support;
using Xunit;

namespace Condux.Otlp.Tests;

/// <summary>
/// Covers the rules the JSON encoding adds to, or takes away from, the standard protobuf JSON mapping.
/// </summary>
public class JsonEncodingTests
{
    /// <summary>
    /// Trace and span ids are hex here, not the base64 the standard mapping uses for bytes. The value
    /// below is valid base64 as well as valid hex, so a decoder taking the standard route produces
    /// twelve different bytes instead of failing, which is how the mistake survives a careless test.
    /// </summary>
    [Fact]
    public void ReadsTraceAndSpanIdsAsHex()
    {
        var record = SingleRecord.DecodeJson("""
            {"traceId":"0af7651916cd43dd8448eb211c80319c","spanId":"b7ad6b7169203331"}
            """);

        Assert.Equal("0AF7651916CD43DD8448EB211C80319C", Convert.ToHexString(record.TraceId));
        Assert.Equal("B7AD6B7169203331", Convert.ToHexString(record.SpanId));
    }

    [Fact]
    public void RejectsATraceIdThatIsNotHex()
    {
        Assert.False(SingleRecord.ParseJson("""{"traceId":"not-hex-at-all!!"}""").IsSuccess);
    }

    [Fact]
    public void RejectsATraceIdWithAnOddNumberOfDigits()
    {
        Assert.False(SingleRecord.ParseJson("""{"traceId":"abc"}""").IsSuccess);
    }

    /// <summary>An empty id is how a record says it belongs to no trace, so it is not an error.</summary>
    [Fact]
    public void ReadsAnEmptyTraceIdAsNoTrace()
    {
        Assert.Empty(SingleRecord.DecodeJson("""{"traceId":""}""").TraceId);
    }

    /// <summary>
    /// A 64-bit integer is written as a decimal string because a JSON number cannot carry the range
    /// exactly, and both spellings are accepted when reading.
    /// </summary>
    [Theory]
    [InlineData("\"1787587793072730000\"")]
    [InlineData("1787587793072730000")]
    public void ReadsA64BitTimestampFromAStringOrANumber(string encoded)
    {
        Assert.Equal(1787587793072730000UL, SingleRecord.DecodeJson($$"""{"timeUnixNano":{{encoded}}}""").TimeUnixNano);
    }

    [Fact]
    public void ReadsAnAttributeValueOfEveryKind()
    {
        var record = SingleRecord.DecodeJson("""
            {"attributes":[
              {"key":"text","value":{"stringValue":"hello"}},
              {"key":"flag","value":{"boolValue":true}},
              {"key":"count","value":{"intValue":"9007199254740993"}},
              {"key":"ratio","value":{"doubleValue":0.25}},
              {"key":"raw","value":{"bytesValue":"AQID"}},
              {"key":"list","value":{"arrayValue":{"values":[{"stringValue":"a"},{"intValue":"2"}]}}},
              {"key":"map","value":{"kvlistValue":{"values":[{"key":"inner","value":{"boolValue":false}}]}}}
            ]}
            """);

        Assert.Equal("hello", SingleRecord.Attribute(record, "text").StringValue);
        Assert.True(SingleRecord.Attribute(record, "flag").BoolValue);
        Assert.Equal(9007199254740993L, SingleRecord.Attribute(record, "count").IntValue);
        Assert.Equal(0.25, SingleRecord.Attribute(record, "ratio").DoubleValue);
        Assert.Equal([1, 2, 3], SingleRecord.Attribute(record, "raw").BytesValue);
        Assert.Equal(2, SingleRecord.Attribute(record, "list").ArrayValue!.Values.Count);
        Assert.Equal("inner", SingleRecord.Attribute(record, "map").KvlistValue!.Values[0].Key);
    }

    /// <summary>
    /// JSON has no literal for these three, so the mapping carries them as strings. A decoder that
    /// accepts only a JSON number rejects a whole export over one attribute, and nothing else in this
    /// suite reads a double from a string.
    /// </summary>
    [Theory]
    [InlineData("\"NaN\"", double.NaN)]
    [InlineData("\"Infinity\"", double.PositiveInfinity)]
    [InlineData("\"-Infinity\"", double.NegativeInfinity)]
    [InlineData("\"0.25\"", 0.25)]
    public void ReadsADoubleWrittenAsAString(string encoded, double expected)
    {
        var json = """{"attributes":[{"key":"ratio","value":{"doubleValue":VALUE}}]}""".Replace("VALUE", encoded);

        Assert.Equal(expected, SingleRecord.Attribute(SingleRecord.DecodeJson(json), "ratio").DoubleValue);
    }

    /// <summary>
    /// The severity travels as its integer, and the scale is ordered so a reader can compare against a
    /// level without knowing every name on it.
    /// </summary>
    [Fact]
    public void ReadsTheSeverityAsANumberOnAnOrderedScale()
    {
        Assert.Equal(SeverityNumber.Error3, SingleRecord.DecodeJson("""{"severityNumber":19}""").SeverityNumber);
        Assert.True(SingleRecord.DecodeJson("""{"severityNumber":21}""").SeverityNumber > SeverityNumber.Error4);
    }

    /// <summary>
    /// A severity the enum does not name still decodes, because the protocol reserves the range for
    /// levels added later and its position on the scale is what a reader acts on.
    /// </summary>
    [Fact]
    public void KeepsASeverityNumberItCannotName()
    {
        Assert.Equal(23, (int)SingleRecord.DecodeJson("""{"severityNumber":23}""").SeverityNumber);
    }

    /// <summary>
    /// The reserved bits of the flags field are not guaranteed to be zero, so a reader that takes the
    /// whole field as the trace flags reads bits the protocol told it to mask off.
    /// </summary>
    [Fact]
    public void MasksTheReservedBitsOutOfTheTraceFlags()
    {
        var record = SingleRecord.DecodeJson("""{"flags":65281}""");

        Assert.Equal(65281u, record.Flags);
        Assert.Equal(1, record.TraceFlags);
    }

    /// <summary>
    /// A value with no member set is the empty value, which the protocol allows and which must not be
    /// confused with an absent value.
    /// </summary>
    [Fact]
    public void ReadsAValueWithNoMemberAsEmptyRatherThanAbsent()
    {
        var record = SingleRecord.DecodeJson("""{"body":{}}""");

        Assert.NotNull(record.Body);
        Assert.Equal(AnyValueKind.None, record.Body.Kind);
    }

    [Fact]
    public void ReadsAnAbsentBodyAsNoBody()
    {
        Assert.Null(SingleRecord.DecodeJson("""{"severityText":"Error"}""").Body);
    }

    /// <summary>A null member means the field's default, which for a message means it is not there.</summary>
    [Fact]
    public void ReadsANullMemberAsTheDefault()
    {
        var record = SingleRecord.DecodeJson("""{"body":null,"severityText":null,"traceId":null}""");

        Assert.Null(record.Body);
        Assert.Equal("", record.SeverityText);
        Assert.Empty(record.TraceId);
    }

    [Fact]
    public void SkipsAMemberItDoesNotKnow()
    {
        var record = SingleRecord.DecodeJson("""{"severityText":"Error","somethingAddedLater":{"deep":[1,2,3]}}""");

        Assert.Equal("Error", record.SeverityText);
    }

    [Fact]
    public void RejectsAMemberWhoseTypeIsWrong()
    {
        Assert.False(SingleRecord.ParseJson("""{"severityText":42}""").IsSuccess);
    }

    [Fact]
    public void RejectsTextThatIsNotJson()
    {
        Assert.False(OtlpLogs.ParseJson("{ not json").IsSuccess);
    }

    [Fact]
    public void RejectsJsonNestedDeeperThanTheDecoderAllows()
    {
        var deep = new string('[', 200) + new string(']', 200);

        Assert.False(OtlpLogs.ParseJson($$"""{"resourceLogs":{{deep}}}""").IsSuccess);
    }

    [Fact]
    public void ReadsAnEmptyObjectAsAnExportWithNothingInIt()
    {
        var result = OtlpLogs.ParseJson("{}");

        Assert.True(result.IsSuccess, result.Error);
        Assert.Empty(result.Value.ResourceLogs);
    }
}
