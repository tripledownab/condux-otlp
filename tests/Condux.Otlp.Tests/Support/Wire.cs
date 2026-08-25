using System.Buffers.Binary;
using Condux.Otlp.Protobuf;

namespace Condux.Otlp.Tests.Support;

/// <summary>
/// Assembles protobuf bytes by hand, for the payloads a real encoder would never produce.
/// </summary>
/// <remarks>
/// This is deliberately not a second encoder. Tags and lengths go through the library's own writer, so
/// there is one varint implementation in the repository. What this adds is the freedom to put a wrong
/// length or an impossible field where a conformant encoder never would, which is the input the guard
/// tests exist to cover. Correct payloads come from the captured fixtures instead.
/// </remarks>
internal static class Wire
{
    internal const int Varint = (int)WireType.Varint;
    internal const int Fixed64 = (int)WireType.Fixed64;
    internal const int LengthDelimited = (int)WireType.LengthDelimited;
    internal const int StartGroup = (int)WireType.StartGroup;
    internal const int Fixed32 = (int)WireType.Fixed32;

    internal static byte[] Tag(int fieldNumber, int wireType)
    {
        var writer = new ProtobufWriter();
        writer.WriteTag(fieldNumber, (WireType)wireType);
        return writer.ToArray();
    }

    internal static byte[] Varints(ulong value)
    {
        var writer = new ProtobufWriter();
        writer.WriteVarint(value);
        return writer.ToArray();
    }

    internal static byte[] VarintField(int fieldNumber, ulong value)
        => Concat(Tag(fieldNumber, Varint), Varints(value));

    internal static byte[] Fixed32Field(int fieldNumber, uint value)
    {
        var payload = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, value);
        return Concat(Tag(fieldNumber, Fixed32), payload);
    }

    internal static byte[] Fixed64Field(int fieldNumber, ulong value)
    {
        var payload = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(payload, value);
        return Concat(Tag(fieldNumber, Fixed64), payload);
    }

    /// <summary>A length-delimited field whose declared length matches its payload.</summary>
    internal static byte[] Bytes(int fieldNumber, byte[] payload)
        => BytesWithLength(fieldNumber, (ulong)payload.Length, payload);

    /// <summary>A length-delimited field whose declared length is whatever the test says it is.</summary>
    internal static byte[] BytesWithLength(int fieldNumber, ulong declaredLength, byte[] payload)
        => Concat(Tag(fieldNumber, LengthDelimited), Varints(declaredLength), payload);

    internal static byte[] Concat(params byte[][] parts)
    {
        var total = new List<byte>();
        foreach (var part in parts)
        {
            total.AddRange(part);
        }

        return [.. total];
    }
}
