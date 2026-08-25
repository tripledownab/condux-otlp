namespace Condux.Otlp.Protobuf;

/// <summary>
/// Decodes the logs messages from the wire format, starting at the export request that forms the body of
/// a call to <c>/v1/logs</c>.
/// </summary>
internal static class LogsDecoder
{
    internal static bool TryReadExportRequest(ReadOnlySpan<byte> bytes, out ExportLogsServiceRequest request)
    {
        request = new ExportLogsServiceRequest();
        return MessageDecoder.TryReadList(bytes, depth: 0, request.ResourceLogs, TryReadResourceLogs);
    }

    private static bool TryReadResourceLogs(ReadOnlySpan<byte> bytes, int depth, out ResourceLogs logs)
    {
        logs = new ResourceLogs();
        return MessageDecoder.TryRead(bytes, depth, logs, ReadResourceLogsField);
    }

    private static bool TryReadScopeLogs(ReadOnlySpan<byte> bytes, int depth, out ScopeLogs logs)
    {
        logs = new ScopeLogs();
        return MessageDecoder.TryRead(bytes, depth, logs, ReadScopeLogsField);
    }

    private static FieldOutcome ReadResourceLogsField(
        ref ProtobufReader reader,
        int fieldNumber,
        WireType wireType,
        ResourceLogs logs,
        int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, ProducerDecoder.TryReadResource, out Resource resource)) return FieldOutcome.Malformed;
                logs.Resource = resource;
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, TryReadScopeLogs, out ScopeLogs scopeLogs)) return FieldOutcome.Malformed;
                logs.ScopeLogs.Add(scopeLogs);
                return FieldOutcome.Handled;
            case 3 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var schemaUrl)) return FieldOutcome.Malformed;
                logs.SchemaUrl = schemaUrl;
                return FieldOutcome.Handled;
            default:
                return FieldOutcome.Unknown;
        }
    }

    private static FieldOutcome ReadScopeLogsField(
        ref ProtobufReader reader,
        int fieldNumber,
        WireType wireType,
        ScopeLogs logs,
        int depth)
    {
        switch (fieldNumber)
        {
            case 1 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, ProducerDecoder.TryReadInstrumentationScope, out InstrumentationScope scope)) return FieldOutcome.Malformed;
                logs.Scope = scope;
                return FieldOutcome.Handled;
            case 2 when wireType == WireType.LengthDelimited:
                if (!MessageDecoder.TryReadNested(ref reader, depth, LogRecordDecoder.TryReadLogRecord, out LogRecord record)) return FieldOutcome.Malformed;
                logs.LogRecords.Add(record);
                return FieldOutcome.Handled;
            case 3 when wireType == WireType.LengthDelimited:
                if (!reader.TryReadString(out var schemaUrl)) return FieldOutcome.Malformed;
                logs.SchemaUrl = schemaUrl;
                return FieldOutcome.Handled;
            default:
                return FieldOutcome.Unknown;
        }
    }
}
