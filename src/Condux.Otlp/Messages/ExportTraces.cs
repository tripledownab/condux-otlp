namespace Condux.Otlp;

/// <summary>
/// The body of an OTLP traces export. This is what a client sends to <c>/v1/traces</c>.
/// <c>opentelemetry.proto.collector.trace.v1.ExportTraceServiceRequest</c>.
/// </summary>
public sealed class ExportTraceServiceRequest
{
    /// <summary>
    /// Field 1. One entry per resource. A collector that batches several origins together sends several.
    /// </summary>
    public List<ResourceSpans> ResourceSpans { get; } = [];
}

/// <summary>
/// The body a server answers a traces export with.
/// <c>opentelemetry.proto.collector.trace.v1.ExportTraceServiceResponse</c>.
/// </summary>
/// <remarks>
/// A full success is an empty message, so the default instance is the right answer when everything was
/// accepted. Encode it with <see cref="ToProtobuf"/> or <see cref="ToJson"/>, matching the encoding the
/// request arrived in: the protocol requires a server to answer in the same content type it received.
/// </remarks>
public sealed class ExportTraceServiceResponse
{
    /// <summary>
    /// Field 1. Set it to report spans the server rejected, or to pass a warning back on an export it
    /// accepted in full. Leaving it absent says everything was accepted.
    /// </summary>
    public ExportTracePartialSuccess? PartialSuccess { get; set; }

    /// <summary>Encodes the response as binary protobuf, for a request that arrived as protobuf.</summary>
    public byte[] ToProtobuf() => ExportResponseEncoder.ToProtobuf(ToParts());

    /// <summary>Encodes the response as OTLP JSON, for a request that arrived as JSON.</summary>
    public string ToJson() => ExportResponseEncoder.ToJson(ToParts(), ExportResponseEncoder.RejectedSpansName);

    private (long Rejected, string ErrorMessage)? ToParts()
        => PartialSuccess is { } partial ? (partial.RejectedSpans, partial.ErrorMessage) : null;
}

/// <summary>
/// What a server rejected, and why.
/// <c>opentelemetry.proto.collector.trace.v1.ExportTracePartialSuccess</c>.
/// </summary>
/// <remarks>
/// Like its logs counterpart, this message has two jobs. With a non-zero <see cref="RejectedSpans"/> it
/// reports a partial rejection. With zero rejected and a non-empty <see cref="ErrorMessage"/> it carries
/// a warning about an export that was accepted in full. Both zero and empty is equivalent to sending
/// nothing, so a sender reads it as a plain success.
/// </remarks>
public sealed class ExportTracePartialSuccess
{
    /// <summary>Field 1. How many spans the server rejected.</summary>
    public long RejectedSpans { get; set; }

    /// <summary>Field 2. A developer-facing message in English, explaining what to do about it.</summary>
    public string ErrorMessage { get; set; } = "";
}
