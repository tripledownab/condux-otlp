namespace Condux.Otlp;

/// <summary>
/// Something that happened at a point during a span. <c>opentelemetry.proto.trace.v1.Span.Event</c>.
/// </summary>
public sealed class SpanEvent
{
    /// <summary>Field 1. When it happened, in nanoseconds since the Unix epoch.</summary>
    public ulong TimeUnixNano { get; set; }

    /// <summary>Field 2. What happened. Required to be non-empty.</summary>
    public string Name { get; set; } = "";

    /// <summary>Field 3. Keys are unique in a well-formed message.</summary>
    public List<KeyValue> Attributes { get; } = [];

    /// <summary>Field 4. Zero means nothing was dropped.</summary>
    public uint DroppedAttributesCount { get; set; }
}

/// <summary>
/// A reference from a span to another span. <c>opentelemetry.proto.trace.v1.Span.Link</c>.
/// </summary>
public sealed class SpanLink
{
    /// <summary>
    /// The mask the protocol defines over <see cref="Flags"/> for the trace flags,
    /// <c>SPAN_FLAGS_TRACE_FLAGS_MASK</c>.
    /// </summary>
    private const uint TraceFlagsMask = 0x000000FF;

    /// <summary>Field 1. The linked span's trace id, sixteen bytes.</summary>
    public byte[] TraceId { get; set; } = [];

    /// <summary>Field 2. The linked span's id, eight bytes.</summary>
    public byte[] SpanId { get; set; } = [];

    /// <summary>Field 3. The W3C <c>tracestate</c> header value the linked span carried, if any.</summary>
    public string TraceState { get; set; } = "";

    /// <summary>Field 4. Keys are unique in a well-formed message.</summary>
    public List<KeyValue> Attributes { get; } = [];

    /// <summary>Field 5. Zero means nothing was dropped.</summary>
    public uint DroppedAttributesCount { get; set; }

    /// <summary>
    /// Field 6. A bit field with the same layout as <see cref="Span.Flags"/>. Read the W3C trace flags
    /// through <see cref="TraceFlags"/>.
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>The W3C trace flags carried in <see cref="Flags"/>, with every other bit masked off.</summary>
    public byte TraceFlags => (byte)(Flags & TraceFlagsMask);
}

/// <summary>
/// How a span's operation ended. <c>opentelemetry.proto.trace.v1.Status</c>.
/// </summary>
/// <remarks>
/// Named for the span because <see cref="Status"/> is already the <c>google.rpc.Status</c> a server
/// answers a failed export with.
/// </remarks>
public sealed class SpanStatus
{
    /// <summary>Field 2. A developer-facing description of the status. Field 1 is reserved.</summary>
    public string Message { get; set; } = "";

    /// <summary>Field 3. A sender may use a number the enum does not name.</summary>
    public SpanStatusCode Code { get; set; }
}

/// <summary>
/// The outcome a span's status records. <c>opentelemetry.proto.trace.v1.Status.StatusCode</c>.
/// </summary>
public enum SpanStatusCode
{
    /// <summary>0. The default: the operation's outcome was not set.</summary>
    Unset = 0,

    /// <summary>1. Set deliberately to say the operation succeeded.</summary>
    Ok = 1,

    /// <summary>2. The operation failed.</summary>
    Error = 2,
}
