namespace Condux.Otlp.Protobuf;

/// <summary>
/// Writes protobuf wire format. It covers only what a service response needs, which is varints, strings
/// and one level of nested message.
/// </summary>
/// <remarks>
/// A field holding its type's default value is left out, as proto3 requires. A reader cannot tell an
/// omitted field from one carrying the default, so writing it would only add bytes.
/// </remarks>
internal sealed class ProtobufWriter
{
    private readonly List<byte> _bytes = [];

    internal void WriteInt64(int fieldNumber, long value)
    {
        if (value == 0)
        {
            return;
        }

        WriteTag(fieldNumber, WireType.Varint);
        WriteVarint(unchecked((ulong)value));
    }

    internal void WriteString(int fieldNumber, string value)
    {
        if (value.Length == 0)
        {
            return;
        }

        var encoded = System.Text.Encoding.UTF8.GetBytes(value);
        WriteTag(fieldNumber, WireType.LengthDelimited);
        WriteVarint((ulong)encoded.Length);
        _bytes.AddRange(encoded);
    }

    internal void WriteMessage(int fieldNumber, ProtobufWriter nested)
    {
        WriteTag(fieldNumber, WireType.LengthDelimited);
        WriteVarint((ulong)nested._bytes.Count);
        _bytes.AddRange(nested._bytes);
    }

    internal byte[] ToArray() => [.. _bytes];

    internal void WriteTag(int fieldNumber, WireType wireType)
        => WriteVarint(((ulong)fieldNumber << 3) | (ulong)wireType);

    internal void WriteVarint(ulong value)
    {
        while (value >= 0x80)
        {
            _bytes.Add((byte)(value | 0x80));
            value >>= 7;
        }

        _bytes.Add((byte)value);
    }
}
