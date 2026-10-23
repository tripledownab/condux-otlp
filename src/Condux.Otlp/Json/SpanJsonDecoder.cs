using System.Text.Json;

namespace Condux.Otlp.Json;

/// <summary>
/// Decodes a span from the JSON encoding, with the event, link and status messages nested inside it.
/// </summary>
/// <remarks>
/// The trace, span and parent span ids are hex, not base64, and enums travel as their integer values.
/// Both are rules of the OTLP JSON encoding rather than of the general protobuf JSON mapping.
/// </remarks>
internal static class SpanJsonDecoder
{
    internal static bool TryReadSpan(JsonElement element, out Span span)
    {
        span = new Span();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryHexBytes(element, "traceId", out var traceId)
            || !JsonFields.TryHexBytes(element, "spanId", out var spanId)
            || !JsonFields.TryString(element, "traceState", out var traceState)
            || !JsonFields.TryHexBytes(element, "parentSpanId", out var parentSpanId)
            || !JsonFields.TryUInt32(element, "flags", out var flags)
            || !JsonFields.TryString(element, "name", out var name)
            || !JsonFields.TryInt32(element, "kind", out var kind)
            || !JsonFields.TryUInt64(element, "startTimeUnixNano", out var start)
            || !JsonFields.TryUInt64(element, "endTimeUnixNano", out var end)
            || !JsonFields.TryList(element, "attributes", span.Attributes, CommonJsonDecoder.TryReadKeyValue)
            || !JsonFields.TryUInt32(element, "droppedAttributesCount", out var droppedAttributes)
            || !JsonFields.TryList(element, "events", span.Events, TryReadEvent)
            || !JsonFields.TryUInt32(element, "droppedEventsCount", out var droppedEvents)
            || !JsonFields.TryList(element, "links", span.Links, TryReadLink)
            || !JsonFields.TryUInt32(element, "droppedLinksCount", out var droppedLinks)
            || !JsonFields.TryNested(element, "status", TryReadStatus, out SpanStatus? status))
        {
            return false;
        }

        span.TraceId = traceId;
        span.SpanId = spanId;
        span.TraceState = traceState;
        span.ParentSpanId = parentSpanId;
        span.Flags = flags;
        span.Name = name;
        span.Kind = (SpanKind)kind;
        span.StartTimeUnixNano = start;
        span.EndTimeUnixNano = end;
        span.DroppedAttributesCount = droppedAttributes;
        span.DroppedEventsCount = droppedEvents;
        span.DroppedLinksCount = droppedLinks;
        span.Status = status;
        return true;
    }

    private static bool TryReadEvent(JsonElement element, out SpanEvent spanEvent)
    {
        spanEvent = new SpanEvent();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryUInt64(element, "timeUnixNano", out var time)
            || !JsonFields.TryString(element, "name", out var name)
            || !JsonFields.TryList(element, "attributes", spanEvent.Attributes, CommonJsonDecoder.TryReadKeyValue)
            || !JsonFields.TryUInt32(element, "droppedAttributesCount", out var dropped))
        {
            return false;
        }

        spanEvent.TimeUnixNano = time;
        spanEvent.Name = name;
        spanEvent.DroppedAttributesCount = dropped;
        return true;
    }

    private static bool TryReadLink(JsonElement element, out SpanLink link)
    {
        link = new SpanLink();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryHexBytes(element, "traceId", out var traceId)
            || !JsonFields.TryHexBytes(element, "spanId", out var spanId)
            || !JsonFields.TryString(element, "traceState", out var traceState)
            || !JsonFields.TryList(element, "attributes", link.Attributes, CommonJsonDecoder.TryReadKeyValue)
            || !JsonFields.TryUInt32(element, "droppedAttributesCount", out var dropped)
            || !JsonFields.TryUInt32(element, "flags", out var flags))
        {
            return false;
        }

        link.TraceId = traceId;
        link.SpanId = spanId;
        link.TraceState = traceState;
        link.DroppedAttributesCount = dropped;
        link.Flags = flags;
        return true;
    }

    private static bool TryReadStatus(JsonElement element, out SpanStatus status)
    {
        status = new SpanStatus();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryString(element, "message", out var message)
            || !JsonFields.TryInt32(element, "code", out var code))
        {
            return false;
        }

        status.Message = message;
        status.Code = (SpanStatusCode)code;
        return true;
    }
}
