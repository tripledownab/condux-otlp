using Condux.Otlp.Tests.Support;
using Xunit;

namespace Condux.Otlp.Tests;

/// <summary>
/// Feeds the protobuf decoder payloads a conforming encoder would never produce.
/// </summary>
/// <remarks>
/// Each test names the guard it removes confidence from. They matter because a receiver decodes whatever
/// arrives on an open port, and a decoder that reads past its buffer or recurses without a bound turns
/// that port into the way in.
/// </remarks>
public class ProtobufGuardTests
{
    // LogRecord.body, the field the nesting tests hang a deeply nested value from.
    private const int BodyField = 5;

    [Fact]
    public void RejectsALengthThatRunsPastTheBuffer()
    {
        // The field claims a thousand bytes and three follow.
        var payload = Wire.BytesWithLength(1, 1000, [1, 2, 3]);

        var result = OtlpLogs.ParseProtobuf(payload);

        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// A 64-bit value needs at most ten bytes at seven bits each. The varint below is well formed apart
    /// from its length, and the field after it is valid, so a decoder without the cap reads straight past
    /// it and succeeds. That is what makes this a test of the cap rather than of running out of buffer.
    /// </summary>
    [Fact]
    public void RejectsAVarintLongerThanAnyValueNeeds()
    {
        const int DroppedAttributesField = 7;
        var elevenBytes = Wire.Concat(Enumerable.Repeat((byte)0x80, 10).ToArray(), [0x01]);
        var record = Wire.Concat(
            Wire.Tag(DroppedAttributesField, Wire.Varint),
            elevenBytes,
            Wire.Bytes(12, "checkout.charge"u8.ToArray()));

        var result = OtlpLogs.ParseProtobuf(SingleRecord.Wrap(record));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void RejectsFieldNumberZero()
    {
        var result = OtlpLogs.ParseProtobuf([0x00, 0x00]);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void RejectsAFixedWidthFieldThatIsCutShort()
    {
        // A fixed64 tag followed by four bytes rather than eight.
        var record = Wire.Concat(Wire.Tag(1, Wire.Fixed64), [1, 2, 3, 4]);

        var result = OtlpLogs.ParseProtobuf(SingleRecord.Wrap(record));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void RejectsTheGroupWireTypeRatherThanGuessingHowLongItIs()
    {
        var payload = Wire.Concat(Wire.Tag(1, Wire.StartGroup), Wire.Varints(1));

        var result = OtlpLogs.ParseProtobuf(payload);

        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// A value nests through arrays without any limit in the protocol, so a payload can ask a decoder to
    /// recurse as deeply as it likes. Every level here is well formed and the innermost value is an
    /// empty one, so depth is the only thing left to reject it for.
    /// </summary>
    [Fact]
    public void RejectsAValueNestedDeeperThanTheLimit()
    {
        var result = OtlpLogs.ParseProtobuf(SingleRecord.Wrap(Wire.Bytes(BodyField, NestedValue(200))));

        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// The other half of the limit: nesting the protocol genuinely uses must still decode. Without this,
    /// a decoder that rejected every nested value would pass the test above.
    /// </summary>
    [Fact]
    public void ReadsAValueNestedWithinTheLimit()
    {
        var result = OtlpLogs.ParseProtobuf(SingleRecord.Wrap(Wire.Bytes(BodyField, NestedValue(3))));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(AnyValueKind.Array, SingleRecord.From(result.Value).Body?.Kind);
    }

    [Fact]
    public void RejectsAStringThatIsNotValidUtf8()
    {
        // 0xC3 opens a two-byte sequence and 0x28 cannot continue it.
        var record = Wire.Bytes(3, [0xC3, 0x28]);

        var result = OtlpLogs.ParseProtobuf(SingleRecord.Wrap(record));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ReadsAnEmptyPayloadAsAnExportWithNothingInIt()
    {
        var result = OtlpLogs.ParseProtobuf([]);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Empty(result.Value.ResourceLogs);
    }

    /// <summary>
    /// Builds a value that is an array containing an array, and so on, ending in an empty value. Each
    /// level is a value holding an array, so it costs two nested messages.
    /// </summary>
    private static byte[] NestedValue(int levels)
    {
        const int ArrayValueField = 5;
        const int ValuesField = 1;
        var value = Array.Empty<byte>();
        for (var level = 0; level < levels; level++)
        {
            value = Wire.Bytes(ArrayValueField, Wire.Bytes(ValuesField, value));
        }

        return value;
    }
}
