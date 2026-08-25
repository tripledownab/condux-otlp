namespace Condux.Otlp.Protobuf;

/// <summary>
/// Decodes the two messages that say where telemetry came from: the resource that produced it and the
/// instrumentation scope that recorded it.
/// </summary>
internal static class ProducerDecoder
{
    internal static bool TryReadResource(ReadOnlySpan<byte> bytes, int depth, out Resource resource)
    {
        resource = new Resource();
        return MessageDecoder.TryRead(bytes, depth, resource, ReadResourceField);
    }

    internal static bool TryReadInstrumentationScope(ReadOnlySpan<byte> bytes, int depth, out InstrumentationScope scope)
    {
        scope = new InstrumentationScope();
        return MessageDecoder.TryRead(bytes, depth, scope, ReadScopeField);
    }

    private static FieldOutcome ReadResourceField(
        ref ProtobufReader reader,
        int fieldNumber,
        WireType wireType,
        Resource resource,
        int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, CommonDecoder.TryReadKeyValue, out KeyValue pair)) return FieldOutcome.Malformed;
                resource.Attributes.Add(pair);
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var dropped)) return FieldOutcome.Malformed;
                resource.DroppedAttributesCount = unchecked((uint)dropped);
                return FieldOutcome.Handled;
            default:
                // Field 3 carries entity references, which the protocol still marks as in development.
                return FieldOutcome.Unknown;
        }
    }

    private static FieldOutcome ReadScopeField(
        ref ProtobufReader reader,
        int fieldNumber,
        WireType wireType,
        InstrumentationScope scope,
        int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var name)) return FieldOutcome.Malformed;
                scope.Name = name;
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var version)) return FieldOutcome.Malformed;
                scope.Version = version;
                return FieldOutcome.Handled;
            case 3 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, CommonDecoder.TryReadKeyValue, out KeyValue pair)) return FieldOutcome.Malformed;
                scope.Attributes.Add(pair);
                return FieldOutcome.Handled;
            case 4 when wireType == WireType.Varint:
                if (!reader.TryReadVarint(out var dropped)) return FieldOutcome.Malformed;
                scope.DroppedAttributesCount = unchecked((uint)dropped);
                return FieldOutcome.Handled;
            default:
                return FieldOutcome.Unknown;
        }
    }
}
