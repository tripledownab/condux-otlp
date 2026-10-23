using Condux.Otlp.Json;
using Condux.Otlp.Protobuf;

namespace Condux.Otlp;

/// <summary>
/// Reads the body of an OTLP traces export, in either encoding the protocol defines.
/// </summary>
/// <remarks>
/// The traces counterpart of <see cref="OtlpLogs"/>, with the same contract: both encodings produce the
/// same message types, and neither method throws.
/// </remarks>
public static class OtlpTraces
{
    // What a failed result says. Each names the shape that was wrong and never quotes the payload,
    // so a caller can log it without logging telemetry it was sent.
    private const string NotProtobuf = "the payload is not a protobuf-encoded OTLP traces export";
    private const string NotAnExport = "the payload is JSON but not a JSON-encoded OTLP traces export";

    /// <summary>
    /// Reads the binary protobuf encoding, which arrives as <c>Content-Type: application/x-protobuf</c>
    /// and is the encoding an OpenTelemetry exporter uses unless it is told otherwise.
    /// </summary>
    public static OtlpReadResult<ExportTraceServiceRequest> ParseProtobuf(ReadOnlySpan<byte> payload)
        => TracesDecoder.TryReadExportRequest(payload, out var request)
            ? OtlpReadResult<ExportTraceServiceRequest>.Ok(request)
            : OtlpReadResult<ExportTraceServiceRequest>.Failed(NotProtobuf);

    /// <summary>
    /// Reads the JSON encoding, which arrives as <c>Content-Type: application/json</c>. The payload is
    /// read in place, so pass the request body straight in rather than decoding it to text first.
    /// </summary>
    public static OtlpReadResult<ExportTraceServiceRequest> ParseJson(ReadOnlyMemory<byte> utf8Payload)
        => JsonPayload.Parse<ExportTraceServiceRequest>(utf8Payload, TracesJsonDecoder.TryReadExportRequest, NotAnExport);

    /// <summary>Reads the JSON encoding from text already decoded from UTF-8.</summary>
    public static OtlpReadResult<ExportTraceServiceRequest> ParseJson(string payload)
        => JsonPayload.Parse<ExportTraceServiceRequest>(payload, TracesJsonDecoder.TryReadExportRequest, NotAnExport);
}
