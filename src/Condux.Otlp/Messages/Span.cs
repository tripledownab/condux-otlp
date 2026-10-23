namespace Condux.Otlp;

/// <summary>One operation in a trace. <c>opentelemetry.proto.trace.v1.Span</c>.</summary>
public sealed class Span
{
    /// <summary>
    /// The mask the protocol defines over <see cref="Flags"/> for the trace flags,
    /// <c>SPAN_FLAGS_TRACE_FLAGS_MASK</c>.
    /// </summary>
    private const uint TraceFlagsMask = 0x000000FF;

    /// <summary>
    /// Field 1. Sixteen bytes, shared by every span of the trace. Required: any other length is invalid,
    /// as is an id of all zeroes. The decoder keeps the bytes as sent and does not check the length.
    /// </summary>
    public byte[] TraceId { get; set; } = [];

    /// <summary>
    /// Field 2. Eight bytes, unique within the trace. Required: any other length is invalid, as is an id
    /// of all zeroes.
    /// </summary>
    public byte[] SpanId { get; set; } = [];

    /// <summary>Field 3. The W3C <c>tracestate</c> header value the span carried, if any.</summary>
    public string TraceState { get; set; } = "";

    /// <summary>
    /// Field 4. The <see cref="SpanId"/> of the parent span. Empty means this is a root span.
    /// </summary>
    public byte[] ParentSpanId { get; set; } = [];

    /// <summary>
    /// Field 16. A bit field. Read the W3C trace flags through <see cref="TraceFlags"/>. Bits 8 and 9 say
    /// whether the parent is remote, and the protocol says a reader must not assume bits 10 to 31 are zero.
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>The W3C trace flags carried in <see cref="Flags"/>, with every other bit masked off.</summary>
    public byte TraceFlags => (byte)(Flags & TraceFlagsMask);

    /// <summary>
    /// Field 5. What the operation was. Required to be non-empty; empty is equivalent to an unknown name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>Field 6. A sender may use a number the enum does not name.</summary>
    public SpanKind Kind { get; set; }

    /// <summary>Field 7. When the operation started, in nanoseconds since the Unix epoch.</summary>
    public ulong StartTimeUnixNano { get; set; }

    /// <summary>
    /// Field 8. When the operation ended, in nanoseconds since the Unix epoch. Expected to be no earlier
    /// than <see cref="StartTimeUnixNano"/>.
    /// </summary>
    public ulong EndTimeUnixNano { get; set; }

    /// <summary>Field 9. Keys are unique in a well-formed message.</summary>
    public List<KeyValue> Attributes { get; } = [];

    /// <summary>Field 10. Zero means nothing was dropped.</summary>
    public uint DroppedAttributesCount { get; set; }

    /// <summary>Field 11. Things that happened during the span, in the order they were recorded.</summary>
    public List<SpanEvent> Events { get; } = [];

    /// <summary>Field 12. Zero means nothing was dropped.</summary>
    public uint DroppedEventsCount { get; set; }

    /// <summary>Field 13. Other spans this one is related to, in this trace or another.</summary>
    public List<SpanLink> Links { get; } = [];

    /// <summary>Field 14. Zero means nothing was dropped.</summary>
    public uint DroppedLinksCount { get; set; }

    /// <summary>
    /// Field 15. How the operation ended. Absent means the same as an unset status.
    /// </summary>
    public SpanStatus? Status { get; set; }
}

/// <summary>
/// What kind of operation a span describes. <c>opentelemetry.proto.trace.v1.Span.SpanKind</c>.
/// </summary>
/// <remarks>
/// A decoder may return a number this enum does not name. The protocol allows a receiver to treat
/// <see cref="Unspecified"/> as <see cref="Internal"/>.
/// </remarks>
public enum SpanKind
{
    /// <summary>0. Not set.</summary>
    Unspecified = 0,

    /// <summary>1. An operation inside an application, not at its boundary.</summary>
    Internal = 1,

    /// <summary>2. The server side of a remote call.</summary>
    Server = 2,

    /// <summary>3. A request to a remote service.</summary>
    Client = 3,

    /// <summary>4. Sending a message to a broker.</summary>
    Producer = 4,

    /// <summary>5. Receiving a message from a broker.</summary>
    Consumer = 5,
}
