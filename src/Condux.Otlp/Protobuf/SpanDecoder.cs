namespace Condux.Otlp.Protobuf;

/// <summary>
/// Decodes <c>opentelemetry.proto.trace.v1.Span</c> from the wire format, with the event, link and status
/// messages nested inside it.
/// </summary>
internal static class SpanDecoder
{
    internal static bool TryReadSpan(ReadOnlySpan<byte> bytes, int depth, out Span span)
    {
        span = new Span();
        return MessageDecoder.TryRead(bytes, depth, span, ReadSpanField);
    }

    private static bool TryReadEvent(ReadOnlySpan<byte> bytes, int depth, out SpanEvent spanEvent)
    {
        spanEvent = new SpanEvent();
        return MessageDecoder.TryRead(bytes, depth, spanEvent, ReadEventField);
    }

    private static bool TryReadLink(ReadOnlySpan<byte> bytes, int depth, out SpanLink link)
    {
        link = new SpanLink();
        return MessageDecoder.TryRead(bytes, depth, link, ReadLinkField);
    }

    private static bool TryReadStatus(ReadOnlySpan<byte> bytes, int depth, out SpanStatus status)
    {
        status = new SpanStatus();
        return MessageDecoder.TryRead(bytes, depth, status, ReadStatusField);
    }

    private static FieldOutcome ReadSpanField(ref ProtobufReader reader, int fieldNumber, WireType wireType, Span span, int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadBytes(out var traceId)) return FieldOutcome.Malformed;
                span.TraceId = traceId;
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadBytes(out var spanId)) return FieldOutcome.Malformed;
                span.SpanId = spanId;
                return FieldOutcome.Handled;
            case 3 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var traceState)) return FieldOutcome.Malformed;
                span.TraceState = traceState;
                return FieldOutcome.Handled;
            case 4 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadBytes(out var parentSpanId)) return FieldOutcome.Malformed;
                span.ParentSpanId = parentSpanId;
                return FieldOutcome.Handled;
            case 5 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var name)) return FieldOutcome.Malformed;
                span.Name = name;
                return FieldOutcome.Handled;
            case 6 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var kind)) return FieldOutcome.Malformed;
                span.Kind = (SpanKind)unchecked((int)kind);
                return FieldOutcome.Handled;
            case 7 when wireType == WireType.Fixed64:
                if (!reader.TryReadFixed64(out var start)) return FieldOutcome.Malformed;
                span.StartTimeUnixNano = start;
                return FieldOutcome.Handled;
            case 8 when wireType == WireType.Fixed64:
                if (!reader.TryReadFixed64(out var end)) return FieldOutcome.Malformed;
                span.EndTimeUnixNano = end;
                return FieldOutcome.Handled;
            case 9 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, CommonDecoder.TryReadKeyValue, out KeyValue pair)) return FieldOutcome.Malformed;
                span.Attributes.Add(pair);
                return FieldOutcome.Handled;
            case 10 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var droppedAttributes)) return FieldOutcome.Malformed;
                span.DroppedAttributesCount = unchecked((uint)droppedAttributes);
                return FieldOutcome.Handled;
            case 11 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, TryReadEvent, out SpanEvent spanEvent)) return FieldOutcome.Malformed;
                span.Events.Add(spanEvent);
                return FieldOutcome.Handled;
            case 12 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var droppedEvents)) return FieldOutcome.Malformed;
                span.DroppedEventsCount = unchecked((uint)droppedEvents);
                return FieldOutcome.Handled;
            case 13 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, TryReadLink, out SpanLink link)) return FieldOutcome.Malformed;
                span.Links.Add(link);
                return FieldOutcome.Handled;
            case 14 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var droppedLinks)) return FieldOutcome.Malformed;
                span.DroppedLinksCount = unchecked((uint)droppedLinks);
                return FieldOutcome.Handled;
            case 15 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, TryReadStatus, out SpanStatus status)) return FieldOutcome.Malformed;
                span.Status = status;
                return FieldOutcome.Handled;
            case 16 when wireType == WireType.Fixed32:
                if (!reader.TryReadFixed32(out var flags)) return FieldOutcome.Malformed;
                span.Flags = flags;
                return FieldOutcome.Handled;
            default:
                return FieldOutcome.Unknown;
        }
    }

    private static FieldOutcome ReadEventField(ref ProtobufReader reader, int fieldNumber, WireType wireType, SpanEvent spanEvent, int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.Fixed64:
                if (!reader.TryReadFixed64(out var time)) return FieldOutcome.Malformed;
                spanEvent.TimeUnixNano = time;
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var name)) return FieldOutcome.Malformed;
                spanEvent.Name = name;
                return FieldOutcome.Handled;
            case 3 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, CommonDecoder.TryReadKeyValue, out KeyValue pair)) return FieldOutcome.Malformed;
                spanEvent.Attributes.Add(pair);
                return FieldOutcome.Handled;
            case 4 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var dropped)) return FieldOutcome.Malformed;
                spanEvent.DroppedAttributesCount = unchecked((uint)dropped);
                return FieldOutcome.Handled;
            default:
                return FieldOutcome.Unknown;
        }
    }

    private static FieldOutcome ReadLinkField(ref ProtobufReader reader, int fieldNumber, WireType wireType, SpanLink link, int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadBytes(out var traceId)) return FieldOutcome.Malformed;
                link.TraceId = traceId;
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadBytes(out var spanId)) return FieldOutcome.Malformed;
                link.SpanId = spanId;
                return FieldOutcome.Handled;
            case 3 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var traceState)) return FieldOutcome.Malformed;
                link.TraceState = traceState;
                return FieldOutcome.Handled;
            case 4 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, CommonDecoder.TryReadKeyValue, out KeyValue pair)) return FieldOutcome.Malformed;
                link.Attributes.Add(pair);
                return FieldOutcome.Handled;
            case 5 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var dropped)) return FieldOutcome.Malformed;
                link.DroppedAttributesCount = unchecked((uint)dropped);
                return FieldOutcome.Handled;
            case 6 when wireType == WireType.Fixed32:
                if (!reader.TryReadFixed32(out var flags)) return FieldOutcome.Malformed;
                link.Flags = flags;
                return FieldOutcome.Handled;
            default:
                return FieldOutcome.Unknown;
        }
    }

    private static FieldOutcome ReadStatusField(ref ProtobufReader reader, int fieldNumber, WireType wireType, SpanStatus status, int depth)
    {
        switch (fieldNumber)
        {
            case 2 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var message)) return FieldOutcome.Malformed;
                status.Message = message;
                return FieldOutcome.Handled;
            case 3 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var code)) return FieldOutcome.Malformed;
                status.Code = (SpanStatusCode)unchecked((int)code);
                return FieldOutcome.Handled;
            default:
                // Field 1 is reserved by the protocol and never appears from a conforming sender.
                return FieldOutcome.Unknown;
        }
    }
}
