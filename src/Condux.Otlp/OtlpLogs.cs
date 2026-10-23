using Condux.Otlp.Json;
using Condux.Otlp.Protobuf;

namespace Condux.Otlp;

/// <summary>
/// Reads the body of an OTLP logs export, in either encoding the protocol defines.
/// </summary>
/// <remarks>
/// Both encodings produce the same message types, so a receiver that accepts both keeps one set of code
/// downstream. Neither method throws: a payload that does not decode comes back as a failed
/// <see cref="OtlpReadResult{TExport}"/>.
/// </remarks>
public static class OtlpLogs
{
    // What a failed result says. Each names the shape that was wrong and never quotes the payload,
    // so a caller can log it without logging telemetry it was sent.
    private const string NotProtobuf = "the payload is not a protobuf-encoded OTLP logs export";
    private const string NotAnExport = "the payload is JSON but not a JSON-encoded OTLP logs export";

    /// <summary>
    /// Reads the binary protobuf encoding, which arrives as <c>Content-Type: application/x-protobuf</c>
    /// and is the encoding an OpenTelemetry exporter uses unless it is told otherwise.
    /// </summary>
    public static OtlpReadResult<ExportLogsServiceRequest> ParseProtobuf(ReadOnlySpan<byte> payload)
        => LogsDecoder.TryReadExportRequest(payload, out var request)
            ? OtlpReadResult<ExportLogsServiceRequest>.Ok(request)
            : OtlpReadResult<ExportLogsServiceRequest>.Failed(NotProtobuf);

    /// <summary>
    /// Reads the JSON encoding, which arrives as <c>Content-Type: application/json</c>. The payload is
    /// read in place, so pass the request body straight in rather than decoding it to text first.
    /// </summary>
    public static OtlpReadResult<ExportLogsServiceRequest> ParseJson(ReadOnlyMemory<byte> utf8Payload)
        => JsonPayload.Parse<ExportLogsServiceRequest>(utf8Payload, LogsJsonDecoder.TryReadExportRequest, NotAnExport);

    /// <summary>Reads the JSON encoding from text already decoded from UTF-8.</summary>
    public static OtlpReadResult<ExportLogsServiceRequest> ParseJson(string payload)
        => JsonPayload.Parse<ExportLogsServiceRequest>(payload, LogsJsonDecoder.TryReadExportRequest, NotAnExport);
}
