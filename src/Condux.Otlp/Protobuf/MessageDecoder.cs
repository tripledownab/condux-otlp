namespace Condux.Otlp.Protobuf;

/// <summary>What a <see cref="FieldReader{TMessage}"/> did with the field it was offered.</summary>
internal enum FieldOutcome
{
    /// <summary>The field was read.</summary>
    Handled,

    /// <summary>The message does not declare this field, or not with this wire type. Step over it.</summary>
    Unknown,

    /// <summary>The bytes do not decode. Abandon the payload.</summary>
    Malformed,
}

/// <summary>Reads one field of <typeparamref name="TMessage"/> into it.</summary>
internal delegate FieldOutcome FieldReader<in TMessage>(
    ref ProtobufReader reader,
    int fieldNumber,
    WireType wireType,
    TMessage message,
    int depth);

/// <summary>Decodes a whole message out of the bytes of one length-delimited field.</summary>
internal delegate bool MessageReader<TMessage>(ReadOnlySpan<byte> bytes, int depth, out TMessage message);

/// <summary>
/// The wire-format loop every message decoder runs. Holding it in one place is what keeps the rules that
/// matter stated once: how deep nesting may go, and that an unrecognised field is stepped over by its
/// wire type rather than guessed at.
/// </summary>
internal static class MessageDecoder
{
    internal static bool TryRead<TMessage>(
        ReadOnlySpan<byte> bytes,
        int depth,
        TMessage message,
        FieldReader<TMessage> readField)
    {
        if (depth > Limits.MaxDepth)
        {
            return false;
        }

        var reader = new ProtobufReader(bytes);
        while (!reader.IsAtEnd)
        {
            if (!reader.TryReadTag(out var fieldNumber, out var wireType))
            {
                return false;
            }

            switch (readField(ref reader, fieldNumber, wireType, message, depth))
            {
                case FieldOutcome.Handled:
                    break;
                case FieldOutcome.Unknown when reader.TrySkip(wireType):
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Decodes a message whose whole content is one repeated message field numbered 1, which is the
    /// shape of the export request and of both of the value collections.
    /// </summary>
    internal static bool TryReadList<TItem>(
        ReadOnlySpan<byte> bytes,
        int depth,
        List<TItem> into,
        MessageReader<TItem> readItem)
        => TryRead(bytes, depth, (into, readItem), ReadListField);

    private static FieldOutcome ReadListField<TItem>(
        ref ProtobufReader reader,
        int fieldNumber,
        WireType wireType,
        (List<TItem> Items, MessageReader<TItem> ReadItem) state,
        int depth)
    {
        if (fieldNumber != 1 || wireType != WireType.LengthDelimited)
        {
            return FieldOutcome.Unknown;
        }

        if (!TryReadNested(ref reader, depth, state.ReadItem, out var item))
        {
            return FieldOutcome.Malformed;
        }

        state.Items.Add(item);
        return FieldOutcome.Handled;
    }

    /// <summary>Reads a nested message field: the length-delimited slice, then one level deeper.</summary>
    internal static bool TryReadNested<TItem>(
        ref ProtobufReader reader,
        int depth,
        MessageReader<TItem> readItem,
        out TItem item)
    {
        item = default!;
        return reader.TryReadLengthDelimited(out var bytes) && readItem(bytes, depth + 1, out item);
    }
}
