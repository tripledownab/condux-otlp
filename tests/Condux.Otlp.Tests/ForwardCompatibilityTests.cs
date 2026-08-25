using Condux.Otlp.Tests.Support;
using Xunit;

namespace Condux.Otlp.Tests;

/// <summary>
/// Covers what happens when a sender knows more of the protocol than this decoder does.
/// </summary>
/// <remarks>
/// The protocol adds fields and does not renumber them, so a decoder that rejected anything it did not
/// recognise would stop working the day a sender upgraded. Stepping over the unknown field by its wire
/// type is what keeps that from happening, and the fields on either side of it must still arrive.
/// </remarks>
public class ForwardCompatibilityTests
{
    [Theory]
    [InlineData(Wire.Varint, new byte[] { 0x96, 0x01 })]
    [InlineData(Wire.Fixed64, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
    [InlineData(Wire.Fixed32, new byte[] { 1, 2, 3, 4 })]
    public void SkipsAnUnknownFieldAndKeepsReading(int wireType, byte[] value)
    {
        const int UnknownField = 900;
        var record = Wire.Concat(
            Wire.Bytes(3, "Error"u8.ToArray()),
            Wire.Tag(UnknownField, wireType),
            value,
            Wire.Bytes(12, "checkout.charge"u8.ToArray()));

        var result = OtlpLogs.ParseProtobuf(SingleRecord.Wrap(record));

        Assert.True(result.IsSuccess, result.Error);
        var decoded = SingleRecord.From(result.Value);
        Assert.Equal("Error", decoded.SeverityText);
        Assert.Equal("checkout.charge", decoded.EventName);
    }

    [Fact]
    public void SkipsAnUnknownLengthDelimitedFieldAndKeepsReading()
    {
        const int UnknownField = 901;
        var record = Wire.Concat(
            Wire.Bytes(3, "Error"u8.ToArray()),
            Wire.Bytes(UnknownField, [9, 9, 9, 9]),
            Wire.Bytes(12, "checkout.charge"u8.ToArray()));

        var result = OtlpLogs.ParseProtobuf(SingleRecord.Wrap(record));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("checkout.charge", SingleRecord.From(result.Value).EventName);
    }

    /// <summary>
    /// A known field arriving with the wrong wire type is treated as unknown rather than read as if the
    /// wire type were right, which would take the decoder off the field boundaries entirely.
    /// </summary>
    [Fact]
    public void TreatsAKnownFieldWithTheWrongWireTypeAsUnknown()
    {
        const int SeverityTextField = 3;
        var record = Wire.Concat(
            Wire.VarintField(SeverityTextField, 7),
            Wire.Bytes(12, "checkout.charge"u8.ToArray()));

        var result = OtlpLogs.ParseProtobuf(SingleRecord.Wrap(record));

        Assert.True(result.IsSuccess, result.Error);
        var decoded = SingleRecord.From(result.Value);
        Assert.Equal("", decoded.SeverityText);
        Assert.Equal("checkout.charge", decoded.EventName);
    }

    /// <summary>
    /// The JSON encoding states the same rule for itself: a receiver must ignore a member it does not
    /// know and read the message as though it were absent.
    /// </summary>
    [Fact]
    public void SkipsAnUnknownJsonMemberAndKeepsReading()
    {
        var result = OtlpLogs.ParseJson("""
            {"resourceLogs":[{"scopeLogs":[{"logRecords":[
              {"severityText":"Error","addedLater":{"nested":[1,2,3]},"eventName":"checkout.charge"}
            ]}],"addedLaterToo":true}]}
            """);

        Assert.True(result.IsSuccess, result.Error);
        var decoded = SingleRecord.From(result.Value);
        Assert.Equal("Error", decoded.SeverityText);
        Assert.Equal("checkout.charge", decoded.EventName);
    }
}
