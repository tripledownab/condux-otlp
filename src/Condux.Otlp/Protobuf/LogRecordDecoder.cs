namespace Condux.Otlp.Protobuf;

/// <summary>Decodes <c>opentelemetry.proto.logs.v1.LogRecord</c> from the wire format.</summary>
internal static class LogRecordDecoder
{
    internal static bool TryReadLogRecord(ReadOnlySpan<byte> bytes, int depth, out LogRecord record)
    {
        record = new LogRecord();
        return MessageDecoder.TryRead(bytes, depth, record, ReadField);
    }

    private static FieldOutcome ReadField(
        ref ProtobufReader reader,
        int fieldNumber,
        WireType wireType,
        LogRecord record,
        int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.Fixed64:
                if (!reader.TryReadFixed64(out var time)) return FieldOutcome.Malformed;
                record.TimeUnixNano = time;
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var severity)) return FieldOutcome.Malformed;
                record.SeverityNumber = (SeverityNumber)unchecked((int)severity);
                return FieldOutcome.Handled;
            case 3 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var severityText)) return FieldOutcome.Malformed;
                record.SeverityText = severityText;
                return FieldOutcome.Handled;
            case 5 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, CommonDecoder.TryReadAnyValue, out AnyValue body)) return FieldOutcome.Malformed;
                record.Body = body;
                return FieldOutcome.Handled;
            case 6 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, CommonDecoder.TryReadKeyValue, out KeyValue pair)) return FieldOutcome.Malformed;
                record.Attributes.Add(pair);
                return FieldOutcome.Handled;
            case 7 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var dropped)) return FieldOutcome.Malformed;
                record.DroppedAttributesCount = unchecked((uint)dropped);
                return FieldOutcome.Handled;
            case 8 when wireType == WireType.Fixed32:
                if (!reader.TryReadFixed32(out var flags)) return FieldOutcome.Malformed;
                record.Flags = flags;
                return FieldOutcome.Handled;
            case 9 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadBytes(out var traceId)) return FieldOutcome.Malformed;
                record.TraceId = traceId;
                return FieldOutcome.Handled;
            case 10 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadBytes(out var spanId)) return FieldOutcome.Malformed;
                record.SpanId = spanId;
                return FieldOutcome.Handled;
            case 11 when wireType == WireType.Fixed64:
                if (!reader.TryReadFixed64(out var observed)) return FieldOutcome.Malformed;
                record.ObservedTimeUnixNano = observed;
                return FieldOutcome.Handled;
            case 12 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var eventName)) return FieldOutcome.Malformed;
                record.EventName = eventName;
                return FieldOutcome.Handled;
            default:
                // Field 4 is reserved by the protocol and never appears from a conforming sender.
                return FieldOutcome.Unknown;
        }
    }
}
