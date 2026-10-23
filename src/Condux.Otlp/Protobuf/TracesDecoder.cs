namespace Condux.Otlp.Protobuf;

/// <summary>
/// Decodes the traces messages from the wire format, starting at the export request that forms the body
/// of a call to <c>/v1/traces</c>. The span itself is in <see cref="SpanDecoder"/>.
/// </summary>
internal static class TracesDecoder
{
    internal static bool TryReadExportRequest(ReadOnlySpan<byte> bytes, out ExportTraceServiceRequest request)
    {
        request = new ExportTraceServiceRequest();
        return MessageDecoder.TryReadList(bytes, depth: 0, request.ResourceSpans, TryReadResourceSpans);
    }

    private static bool TryReadResourceSpans(ReadOnlySpan<byte> bytes, int depth, out ResourceSpans spans)
    {
        spans = new ResourceSpans();
        return MessageDecoder.TryRead(bytes, depth, spans, ReadResourceSpansField);
    }

    private static bool TryReadScopeSpans(ReadOnlySpan<byte> bytes, int depth, out ScopeSpans spans)
    {
        spans = new ScopeSpans();
        return MessageDecoder.TryRead(bytes, depth, spans, ReadScopeSpansField);
    }

    private static FieldOutcome ReadResourceSpansField(
        ref ProtobufReader reader,
        int fieldNumber,
        WireType wireType,
        ResourceSpans spans,
        int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, ProducerDecoder.TryReadResource, out Resource resource)) return FieldOutcome.Malformed;
                spans.Resource = resource;
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, TryReadScopeSpans, out ScopeSpans scopeSpans)) return FieldOutcome.Malformed;
                spans.ScopeSpans.Add(scopeSpans);
                return FieldOutcome.Handled;
            case 3 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var schemaUrl)) return FieldOutcome.Malformed;
                spans.SchemaUrl = schemaUrl;
                return FieldOutcome.Handled;
            default:
                return FieldOutcome.Unknown;
        }
    }

    private static FieldOutcome ReadScopeSpansField(
        ref ProtobufReader reader,
        int fieldNumber,
        WireType wireType,
        ScopeSpans spans,
        int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, ProducerDecoder.TryReadInstrumentationScope, out InstrumentationScope scope)) return FieldOutcome.Malformed;
                spans.Scope = scope;
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, SpanDecoder.TryReadSpan, out Span span)) return FieldOutcome.Malformed;
                spans.Spans.Add(span);
                return FieldOutcome.Handled;
            case 3 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var schemaUrl)) return FieldOutcome.Malformed;
                spans.SchemaUrl = schemaUrl;
                return FieldOutcome.Handled;
            default:
                return FieldOutcome.Unknown;
        }
    }
}
