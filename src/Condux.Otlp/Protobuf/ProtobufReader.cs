using System.Buffers.Binary;
using System.Text;
using System.Text.Unicode;

namespace Condux.Otlp.Protobuf;

/// <summary>
/// Reads protobuf wire-format primitives out of a buffer.
/// </summary>
/// <remarks>
/// Every method returns false instead of throwing, and every read is checked against what is left in the
/// buffer before it happens. A length-delimited field comes back as a slice of the caller's own buffer,
/// so a length a payload claims cannot turn into an allocation of that size.
/// </remarks>
internal ref struct ProtobufReader
{
    // The wire format gives a field number 29 bits, so anything above this was never a tag.
    private const ulong MaxFieldNumber = (1UL << 29) - 1;

    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    internal ProtobufReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    internal readonly bool IsAtEnd => _position >= _buffer.Length;

    private readonly int Remaining => _buffer.Length - _position;

    /// <summary>Reads a field tag: the field number and the wire type of its value.</summary>
    internal bool TryReadTag(out int fieldNumber, out WireType wireType)
    {
        fieldNumber = 0;
        wireType = WireType.Varint;
        if (!TryReadVarint(out var tag))
        {
            return false;
        }

        // Field numbers run from 1 to 2^29-1. Zero is not a legal field number, and anything above the
        // range means the varint just read was not a tag.
        var number = tag >> 3;
        if (number is < 1 or > MaxFieldNumber)
        {
            return false;
        }

        fieldNumber = (int)number;
        wireType = (WireType)(tag & 0b111);
        return true;
    }

    internal bool TryReadVarint(out ulong value)
    {
        value = 0;
        var shift = 0;
        for (var read = 0; read < Limits.MaxVarintBytes; read++)
        {
            if (_position >= _buffer.Length)
            {
                value = 0;
                return false;
            }

            var current = _buffer[_position++];
            value |= (ulong)(current & 0x7F) << shift;
            if ((current & 0x80) == 0)
            {
                return true;
            }

            shift += 7;
        }

        // Ten bytes went by and the last one still asked for more, so the value is not a 64-bit varint.
        value = 0;
        return false;
    }

    internal bool TryReadFixed32(out uint value)
    {
        if (Remaining < sizeof(uint))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(_position, sizeof(uint)));
        _position += sizeof(uint);
        return true;
    }

    internal bool TryReadFixed64(out ulong value)
    {
        if (Remaining < sizeof(ulong))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.Slice(_position, sizeof(ulong)));
        _position += sizeof(ulong);
        return true;
    }

    /// <summary>Reads a double, which the wire format carries as a fixed64.</summary>
    internal bool TryReadDouble(out double value)
    {
        if (!TryReadFixed64(out var bits))
        {
            value = 0;
            return false;
        }

        value = BitConverter.Int64BitsToDouble(unchecked((long)bits));
        return true;
    }

    /// <summary>Reads a length-delimited field as a slice of the underlying buffer.</summary>
    internal bool TryReadLengthDelimited(out ReadOnlySpan<byte> value)
    {
        value = default;
        if (!TryReadVarint(out var length))
        {
            return false;
        }

        // The length is compared against what is actually left before anything is sliced, so a payload
        // claiming four gigabytes in a twenty-byte buffer is rejected rather than acted on.
        if (length > (ulong)Remaining)
        {
            return false;
        }

        value = _buffer.Slice(_position, (int)length);
        _position += (int)length;
        return true;
    }

    /// <summary>
    /// Reads a string field. The wire format requires a string to be valid UTF-8, so one that is not is
    /// rejected rather than repaired: a decoder that substitutes replacement characters hands the caller
    /// data the sender never sent.
    /// </summary>
    internal bool TryReadString(out string value)
    {
        value = "";
        if (!TryReadLengthDelimited(out var bytes))
        {
            return false;
        }

        if (bytes.IsEmpty)
        {
            return true;
        }

        if (!Utf8.IsValid(bytes))
        {
            return false;
        }

        value = Encoding.UTF8.GetString(bytes);
        return true;
    }

    /// <summary>Reads a bytes field, copied out of the buffer so it outlives the read.</summary>
    internal bool TryReadBytes(out byte[] value)
    {
        value = [];
        if (!TryReadLengthDelimited(out var bytes))
        {
            return false;
        }

        value = bytes.ToArray();
        return true;
    }

    /// <summary>
    /// Steps over a field the caller does not know, using its wire type alone. This is what keeps a
    /// decoder working against a newer sender: fields added later are skipped, not guessed at.
    /// </summary>
    internal bool TrySkip(WireType wireType) => wireType switch
    {
        WireType.Varint => TryReadVarint(out _),
        WireType.Fixed64 => TryReadFixed64(out _),
        WireType.LengthDelimited => TryReadLengthDelimited(out _),
        WireType.Fixed32 => TryReadFixed32(out _),
        // Groups were removed in proto3 and no message here uses them. Skipping one means matching its
        // nested start and end tags, so a payload that contains one is rejected rather than half read.
        _ => false,
    };
}
