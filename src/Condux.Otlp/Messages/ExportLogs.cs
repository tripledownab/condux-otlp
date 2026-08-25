namespace Condux.Otlp;

/// <summary>
/// The body of an OTLP logs export. This is what a client sends to <c>/v1/logs</c>.
/// <c>opentelemetry.proto.collector.logs.v1.ExportLogsServiceRequest</c>.
/// </summary>
public sealed class ExportLogsServiceRequest
{
    /// <summary>
    /// Field 1. One entry per resource. A collector that batches several origins together sends several.
    /// </summary>
    public List<ResourceLogs> ResourceLogs { get; } = [];
}

/// <summary>
/// The body a server answers an export with.
/// <c>opentelemetry.proto.collector.logs.v1.ExportLogsServiceResponse</c>.
/// </summary>
/// <remarks>
/// A full success is an empty message, so the default instance is the right answer when everything was
/// accepted. Encode it with <see cref="ToProtobuf"/> or <see cref="ToJson"/>, matching the encoding the
/// request arrived in: the protocol requires a server to answer in the same content type it received.
/// </remarks>
public sealed class ExportLogsServiceResponse
{
    /// <summary>
    /// Field 1. Set it to report records the server rejected, or to pass a warning back on an export it
    /// accepted in full. Leaving it absent says everything was accepted.
    /// </summary>
    public ExportLogsPartialSuccess? PartialSuccess { get; set; }

    /// <summary>Encodes the response as binary protobuf, for a request that arrived as protobuf.</summary>
    public byte[] ToProtobuf() => ExportLogsResponseEncoder.ToProtobuf(this);

    /// <summary>Encodes the response as OTLP JSON, for a request that arrived as JSON.</summary>
    public string ToJson() => ExportLogsResponseEncoder.ToJson(this);
}

/// <summary>
/// What a server rejected, and why.
/// <c>opentelemetry.proto.collector.logs.v1.ExportLogsPartialSuccess</c>.
/// </summary>
/// <remarks>
/// The protocol gives this message two jobs. With a non-zero <see cref="RejectedLogRecords"/> it reports
/// a partial rejection. With zero rejected and a non-empty <see cref="ErrorMessage"/> it carries a
/// warning about an export that was accepted in full. Both zero and empty is equivalent to sending
/// nothing, so a sender reads it as a plain success.
/// </remarks>
public sealed class ExportLogsPartialSuccess
{
    /// <summary>Field 1. How many records the server rejected.</summary>
    public long RejectedLogRecords { get; set; }

    /// <summary>Field 2. A developer-facing message in English, explaining what to do about it.</summary>
    public string ErrorMessage { get; set; } = "";
}
