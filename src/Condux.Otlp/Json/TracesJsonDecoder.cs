using System.Text.Json;

namespace Condux.Otlp.Json;

/// <summary>
/// Decodes the traces messages from the JSON encoding, down to the scope. The span itself is in
/// <see cref="SpanJsonDecoder"/>.
/// </summary>
internal static class TracesJsonDecoder
{
    internal static bool TryReadExportRequest(JsonElement element, out ExportTraceServiceRequest request)
    {
        request = new ExportTraceServiceRequest();
        return element.ValueKind == JsonValueKind.Object
            && JsonFields.TryList(element, "resourceSpans", request.ResourceSpans, TryReadResourceSpans);
    }

    private static bool TryReadResourceSpans(JsonElement element, out ResourceSpans spans)
    {
        spans = new ResourceSpans();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryNested(element, "resource", CommonJsonDecoder.TryReadResource, out Resource? resource)
            || !JsonFields.TryList(element, "scopeSpans", spans.ScopeSpans, TryReadScopeSpans)
            || !JsonFields.TryString(element, "schemaUrl", out var schemaUrl))
        {
            return false;
        }

        spans.Resource = resource;
        spans.SchemaUrl = schemaUrl;
        return true;
    }

    private static bool TryReadScopeSpans(JsonElement element, out ScopeSpans spans)
    {
        spans = new ScopeSpans();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryNested(element, "scope", CommonJsonDecoder.TryReadInstrumentationScope, out InstrumentationScope? scope)
            || !JsonFields.TryList(element, "spans", spans.Spans, SpanJsonDecoder.TryReadSpan)
            || !JsonFields.TryString(element, "schemaUrl", out var schemaUrl))
        {
            return false;
        }

        spans.Scope = scope;
        spans.SchemaUrl = schemaUrl;
        return true;
    }
}
