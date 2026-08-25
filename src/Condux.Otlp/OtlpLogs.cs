using System.Text.Json;
using Condux.Otlp.Json;
using Condux.Otlp.Protobuf;

namespace Condux.Otlp;

/// <summary>
/// Reads the body of an OTLP logs export, in either encoding the protocol defines.
/// </summary>
/// <remarks>
/// Both encodings produce the same message types, so a receiver that accepts both keeps one set of code
/// downstream. Neither method throws: a payload that does not decode comes back as a failed
/// <see cref="OtlpReadResult"/>.
/// </remarks>
public static class OtlpLogs
{
    // What a failed result says. Each names the shape that was wrong and never quotes the payload,
    // so a caller can log it without logging telemetry it was sent.
    private const string NotProtobuf = "the payload is not a protobuf-encoded OTLP logs export";
    private const string NotJson = "the payload is not well-formed JSON, or nests deeper than the decoder allows";
    private const string NotAnExport = "the payload is JSON but not a JSON-encoded OTLP logs export";

    /// <summary>
    /// Reads the binary protobuf encoding, which arrives as <c>Content-Type: application/x-protobuf</c>
    /// and is the encoding an OpenTelemetry exporter uses unless it is told otherwise.
    /// </summary>
    public static OtlpReadResult ParseProtobuf(ReadOnlySpan<byte> payload)
        => LogsDecoder.TryReadExportRequest(payload, out var request)
            ? OtlpReadResult.Ok(request)
            : OtlpReadResult.Failed(NotProtobuf);

    /// <summary>
    /// Reads the JSON encoding, which arrives as <c>Content-Type: application/json</c>. The payload is
    /// read in place, so pass the request body straight in rather than decoding it to text first.
    /// </summary>
    public static OtlpReadResult ParseJson(ReadOnlyMemory<byte> utf8Payload)
    {
        try
        {
            using var document = JsonDocument.Parse(utf8Payload, DocumentOptions);
            return Read(document);
        }
        catch (JsonException)
        {
            return OtlpReadResult.Failed(NotJson);
        }
    }

    /// <summary>Reads the JSON encoding from text already decoded from UTF-8.</summary>
    public static OtlpReadResult ParseJson(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload, DocumentOptions);
            return Read(document);
        }
        catch (JsonException)
        {
            return OtlpReadResult.Failed(NotJson);
        }
    }

    private static OtlpReadResult Read(JsonDocument document)
        => LogsJsonDecoder.TryReadExportRequest(document.RootElement, out var request)
            ? OtlpReadResult.Ok(request)
            : OtlpReadResult.Failed(NotAnExport);

    /// <summary>
    /// Nesting is bounded at parse time rather than during decoding, because the decoder follows the
    /// document's own structure and so can go no deeper than the document does.
    /// </summary>
    private static readonly JsonDocumentOptions DocumentOptions = new() { MaxDepth = Limits.MaxDepth };
}
