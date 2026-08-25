using System.Globalization;
using System.Text;

namespace Condux.Otlp.Tests.Support;

/// <summary>
/// Renders a decoded export as deterministic text, so two decodes can be compared as a whole and a
/// failure reads as a diff rather than as an index that did not match.
/// </summary>
/// <remarks>
/// It lives in the tests, not the library, and it renders every field the decoders set. A field it
/// skipped could differ between two encodings without any test noticing.
/// </remarks>
internal static class MessageDump
{
    internal static string Render(ExportLogsServiceRequest request)
    {
        var text = new StringBuilder();
        foreach (var resourceLogs in request.ResourceLogs)
        {
            text.AppendLine($"resourceLogs schemaUrl={resourceLogs.SchemaUrl}");
            if (resourceLogs.Resource is not null)
            {
                text.AppendLine($"  resource dropped={resourceLogs.Resource.DroppedAttributesCount}");
                Attributes(text, resourceLogs.Resource.Attributes, "    ");
            }

            foreach (var scopeLogs in resourceLogs.ScopeLogs)
            {
                text.AppendLine($"  scopeLogs schemaUrl={scopeLogs.SchemaUrl}");
                if (scopeLogs.Scope is not null)
                {
                    text.AppendLine($"    scope name={scopeLogs.Scope.Name} version={scopeLogs.Scope.Version} dropped={scopeLogs.Scope.DroppedAttributesCount}");
                    Attributes(text, scopeLogs.Scope.Attributes, "      ");
                }

                foreach (var record in scopeLogs.LogRecords)
                {
                    Record(text, record);
                }
            }
        }

        return text.ToString();
    }

    private static void Record(StringBuilder text, LogRecord record)
    {
        text.AppendLine($"    record time={record.TimeUnixNano} observed={record.ObservedTimeUnixNano}");
        text.AppendLine($"      severity={(int)record.SeverityNumber} text={record.SeverityText}");
        text.AppendLine($"      eventName={record.EventName} flags={record.Flags} traceFlags={record.TraceFlags}");
        text.AppendLine($"      traceId={Convert.ToHexString(record.TraceId)} spanId={Convert.ToHexString(record.SpanId)}");
        text.AppendLine($"      dropped={record.DroppedAttributesCount}");
        text.AppendLine($"      body={Value(record.Body)}");
        Attributes(text, record.Attributes, "      ");
    }

    private static void Attributes(StringBuilder text, List<KeyValue> attributes, string indent)
    {
        foreach (var pair in attributes)
        {
            text.AppendLine($"{indent}{pair.Key}={Value(pair.Value)}");
        }
    }

    private static string Value(AnyValue? value)
    {
        if (value is null)
        {
            return "<none>";
        }

        return value.Kind switch
        {
            AnyValueKind.None => "<empty>",
            AnyValueKind.String => $"string:{value.StringValue}",
            AnyValueKind.Bool => $"bool:{value.BoolValue}",
            AnyValueKind.Int => $"int:{value.IntValue.ToString(CultureInfo.InvariantCulture)}",
            AnyValueKind.Double => $"double:{value.DoubleValue.ToString("R", CultureInfo.InvariantCulture)}",
            AnyValueKind.Bytes => $"bytes:{Convert.ToHexString(value.BytesValue ?? [])}",
            AnyValueKind.Array => $"array:[{string.Join(",", value.ArrayValue!.Values.Select(Value))}]",
            AnyValueKind.Kvlist => $"kvlist:[{string.Join(",", value.KvlistValue!.Values.Select(Pair))}]",
            _ => "<unknown>",
        };
    }

    /// <summary>
    /// A composite is rendered element by element rather than by its type name. Rendering it as one
    /// opaque token would make two different arrays compare equal, and the whole point of this dump is
    /// that two decodes of one export can be compared in full.
    /// </summary>
    private static string Pair(KeyValue pair) => $"{pair.Key}={Value(pair.Value)}";
}
