using Condux.Otlp.Protobuf;
using Xunit;

namespace Condux.Otlp.Tests;

/// <summary>
/// Covers the status a server answers a failed export with. The protocol requires one on every 4xx and
/// 5xx, in the encoding the request arrived in, so both encodings are held to their bytes here rather
/// than to a round trip: the package writes this message and never reads it.
/// </summary>
public class StatusTests
{
    [Fact]
    public void EncodesTheCodeAndMessageAsProtobuf()
    {
        var status = new Status { Code = (int)StatusCode.InvalidArgument, Message = "malformed export" };

        // Reading the bytes back checks the tags rather than only the values: code is field 1 as a
        // varint, message is field 2 as length-delimited.
        var reader = new ProtobufReader(status.ToProtobuf());
        Assert.True(reader.TryReadTag(out var field, out var wireType));
        Assert.Equal((1, WireType.Varint), (field, wireType));
        Assert.True(reader.TryReadVarint(out var code));
        Assert.Equal(3UL, code);
        Assert.True(reader.TryReadTag(out field, out wireType));
        Assert.Equal((2, WireType.LengthDelimited), (field, wireType));
        Assert.True(reader.TryReadString(out var message));
        Assert.Equal("malformed export", message);
        Assert.True(reader.IsAtEnd);
    }

    /// <summary>
    /// The code is an int32, so it travels as a JSON number. The 64-bit counts elsewhere in the protocol
    /// travel as decimal strings instead, and reading this as one would be wrong.
    /// </summary>
    [Fact]
    public void EncodesTheCodeAsANumberInJson()
    {
        var status = new Status { Code = (int)StatusCode.ResourceExhausted, Message = "over the ingest limit" };

        Assert.Equal("""{"code":8,"message":"over the ingest limit"}""", status.ToJson());
    }

    /// <summary>
    /// proto3 omits a field holding its type's default, and a reader cannot tell an omitted field from
    /// one carrying that default, so writing it would only add bytes.
    /// </summary>
    [Fact]
    public void OmitsFieldsHoldingTheirDefault()
    {
        var status = new Status();

        Assert.Empty(status.ToProtobuf());
        Assert.Equal("{}", status.ToJson());
    }

    /// <summary>
    /// A code the enum does not name is legal: the field is an int32 and the code set can grow, so the
    /// encoder must not narrow it to what this version happens to know.
    /// </summary>
    [Fact]
    public void CarriesACodeTheEnumDoesNotName()
    {
        var status = new Status { Code = 99 };

        Assert.Equal("""{"code":99}""", status.ToJson());

        var reader = new ProtobufReader(status.ToProtobuf());
        Assert.True(reader.TryReadTag(out var field, out var wireType));
        Assert.Equal((1, WireType.Varint), (field, wireType));
        Assert.True(reader.TryReadVarint(out var code));
        Assert.Equal(99UL, code);
    }
}
