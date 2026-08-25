namespace Condux.Otlp;

/// <summary>One log record. <c>opentelemetry.proto.logs.v1.LogRecord</c>.</summary>
public sealed class LogRecord
{
    /// <summary>
    /// The mask the protocol defines over <see cref="Flags"/> for the trace flags,
    /// <c>LOG_RECORD_FLAGS_TRACE_FLAGS_MASK</c>. The other 24 bits are reserved.
    /// </summary>
    private const uint TraceFlagsMask = 0x000000FF;

    /// <summary>
    /// Field 1. When the event happened, in nanoseconds since the Unix epoch. Zero means the time is
    /// unknown or missing. For a consumer that keeps only one timestamp, the protocol recommends using
    /// this field when it is present and <see cref="ObservedTimeUnixNano"/> otherwise.
    /// </summary>
    public ulong TimeUnixNano { get; set; }

    /// <summary>
    /// Field 11. When the collection system saw the event, in nanoseconds since the Unix epoch. Set once
    /// the event is observed, so it is normally present even when <see cref="TimeUnixNano"/> is not.
    /// </summary>
    public ulong ObservedTimeUnixNano { get; set; }

    /// <summary>
    /// Field 2. The severity on the normalized scale. A sender may use a number the enum does not name.
    /// </summary>
    public SeverityNumber SeverityNumber { get; set; }

    /// <summary>Field 3. The level as the source itself spelled it, for example <c>WARN</c>.</summary>
    public string SeverityText { get; set; } = "";

    /// <summary>Field 5. The body of the record. Absent when the record carries none.</summary>
    public AnyValue? Body { get; set; }

    /// <summary>Field 6. Keys are unique in a well-formed message.</summary>
    public List<KeyValue> Attributes { get; } = [];

    /// <summary>Field 7. Zero means nothing was dropped.</summary>
    public uint DroppedAttributesCount { get; set; }

    /// <summary>
    /// Field 8. A bit field. Read it through <see cref="TraceFlags"/> rather than directly: the top 24
    /// bits are reserved, and the protocol says a reader must not assume they are zero.
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// The W3C trace flags carried in <see cref="Flags"/>, with the reserved bits masked off.
    /// </summary>
    public byte TraceFlags => (byte)(Flags & TraceFlagsMask);

    /// <summary>
    /// Field 9. Sixteen bytes, or empty when the record belongs to no trace. Any other length is invalid,
    /// as is an id of all zeroes. The protocol says a receiver should assume that a record carrying an
    /// absent or invalid id belongs to no trace.
    /// </summary>
    public byte[] TraceId { get; set; } = [];

    /// <summary>
    /// Field 10. Eight bytes, or empty when the record belongs to no span. Any other length is invalid,
    /// as is an id of all zeroes.
    /// </summary>
    public byte[] SpanId { get; set; } = [];

    /// <summary>
    /// Field 12. Names the category of event. Its presence is what makes a record an event rather than a
    /// plain log line.
    /// </summary>
    public string EventName { get; set; } = "";

}
