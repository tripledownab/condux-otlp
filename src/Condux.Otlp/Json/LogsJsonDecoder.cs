using System.Text.Json;

namespace Condux.Otlp.Json;

/// <summary>Decodes the logs messages from the JSON encoding.</summary>
internal static class LogsJsonDecoder
{
    internal static bool TryReadExportRequest(JsonElement element, out ExportLogsServiceRequest request)
    {
        request = new ExportLogsServiceRequest();
        return element.ValueKind == JsonValueKind.Object
            && JsonFields.TryList(element, "resourceLogs", request.ResourceLogs, TryReadResourceLogs);
    }

    private static bool TryReadResourceLogs(JsonElement element, out ResourceLogs logs)
    {
        logs = new ResourceLogs();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryNested(element, "resource", CommonJsonDecoder.TryReadResource, out Resource? resource)
            || !JsonFields.TryList(element, "scopeLogs", logs.ScopeLogs, TryReadScopeLogs)
            || !JsonFields.TryString(element, "schemaUrl", out var schemaUrl))
        {
            return false;
        }

        logs.Resource = resource;
        logs.SchemaUrl = schemaUrl;
        return true;
    }

    private static bool TryReadScopeLogs(JsonElement element, out ScopeLogs logs)
    {
        logs = new ScopeLogs();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryNested(element, "scope", CommonJsonDecoder.TryReadInstrumentationScope, out InstrumentationScope? scope)
            || !JsonFields.TryList(element, "logRecords", logs.LogRecords, TryReadLogRecord)
            || !JsonFields.TryString(element, "schemaUrl", out var schemaUrl))
        {
            return false;
        }

        logs.Scope = scope;
        logs.SchemaUrl = schemaUrl;
        return true;
    }

    private static bool TryReadLogRecord(JsonElement element, out LogRecord record)
    {
        record = new LogRecord();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryUInt64(element, "timeUnixNano", out var time)
            || !JsonFields.TryUInt64(element, "observedTimeUnixNano", out var observed)
            // The encoding requires an enum to travel as its integer value, never as its name.
            || !JsonFields.TryInt32(element, "severityNumber", out var severity)
            || !JsonFields.TryString(element, "severityText", out var severityText)
            || !JsonFields.TryNested(element, "body", CommonJsonDecoder.TryReadAnyValue, out AnyValue? body)
            || !JsonFields.TryList(element, "attributes", record.Attributes, CommonJsonDecoder.TryReadKeyValue)
            || !JsonFields.TryUInt32(element, "droppedAttributesCount", out var dropped)
            || !JsonFields.TryUInt32(element, "flags", out var flags)
            || !JsonFields.TryHexBytes(element, "traceId", out var traceId)
            || !JsonFields.TryHexBytes(element, "spanId", out var spanId)
            || !JsonFields.TryString(element, "eventName", out var eventName))
        {
            return false;
        }

        record.TimeUnixNano = time;
        record.ObservedTimeUnixNano = observed;
        record.SeverityNumber = (SeverityNumber)severity;
        record.SeverityText = severityText;
        record.Body = body;
        record.DroppedAttributesCount = dropped;
        record.Flags = flags;
        record.TraceId = traceId;
        record.SpanId = spanId;
        record.EventName = eventName;
        return true;
    }
}
