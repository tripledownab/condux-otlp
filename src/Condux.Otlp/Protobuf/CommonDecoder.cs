namespace Condux.Otlp.Protobuf;

/// <summary>Decodes the value model of <c>opentelemetry.proto.common.v1</c> from the wire format.</summary>
internal static class CommonDecoder
{
    internal static bool TryReadAnyValue(ReadOnlySpan<byte> bytes, int depth, out AnyValue value)
    {
        value = new AnyValue();
        return MessageDecoder.TryRead(bytes, depth, value, ReadAnyValueField);
    }

    private static bool TryReadArrayValue(ReadOnlySpan<byte> bytes, int depth, out ArrayValue value)
    {
        value = new ArrayValue();
        return MessageDecoder.TryReadList(bytes, depth, value.Values, TryReadAnyValue);
    }

    private static bool TryReadKeyValueList(ReadOnlySpan<byte> bytes, int depth, out KeyValueList value)
    {
        value = new KeyValueList();
        return MessageDecoder.TryReadList(bytes, depth, value.Values, TryReadKeyValue);
    }

    internal static bool TryReadKeyValue(ReadOnlySpan<byte> bytes, int depth, out KeyValue pair)
    {
        pair = new KeyValue();
        return MessageDecoder.TryRead(bytes, depth, pair, ReadKeyValueField);
    }

    private static FieldOutcome ReadAnyValueField(
        ref ProtobufReader reader,
        int fieldNumber,
        WireType wireType,
        AnyValue value,
        int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var text)) return FieldOutcome.Malformed;
                value.Kind = AnyValueKind.String;
                value.StringValue = text;
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var flag)) return FieldOutcome.Malformed;
                value.Kind = AnyValueKind.Bool;
                value.BoolValue = flag != 0;
                return FieldOutcome.Handled;
            case 3 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var number)) return FieldOutcome.Malformed;
                value.Kind = AnyValueKind.Int;
                value.IntValue = unchecked((long)number);
                return FieldOutcome.Handled;
            case 4 when wireType == WireType.Fixed64:
                if (!reader.TryReadDouble(out var fraction)) return FieldOutcome.Malformed;
                value.Kind = AnyValueKind.Double;
                value.DoubleValue = fraction;
                return FieldOutcome.Handled;
            case 5 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, TryReadArrayValue, out ArrayValue array)) return FieldOutcome.Malformed;
                value.Kind = AnyValueKind.Array;
                value.ArrayValue = array;
                return FieldOutcome.Handled;
            case 6 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, TryReadKeyValueList, out KeyValueList list)) return FieldOutcome.Malformed;
                value.Kind = AnyValueKind.Kvlist;
                value.KvlistValue = list;
                return FieldOutcome.Handled;
            case 7 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadBytes(out var raw)) return FieldOutcome.Malformed;
                value.Kind = AnyValueKind.Bytes;
                value.BytesValue = raw;
                return FieldOutcome.Handled;
            default:
                // Field 8 lands here. It indexes the profiling signal's string table, and the protocol
                // tells a receiver of any other signal to carry on as if the field were absent.
                return FieldOutcome.Unknown;
        }
    }

    private static FieldOutcome ReadKeyValueField(
        ref ProtobufReader reader,
        int fieldNumber,
        WireType wireType,
        KeyValue pair,
        int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var key)) return FieldOutcome.Malformed;
                pair.Key = key;
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, TryReadAnyValue, out AnyValue value)) return FieldOutcome.Malformed;
                pair.Value = value;
                return FieldOutcome.Handled;
            default:
                // Field 3 indexes the profiling signal's string table for the key, skipped for the same
                // reason as field 8 of AnyValue.
                return FieldOutcome.Unknown;
        }
    }
}
