namespace Condux.Otlp;

/// <summary>
/// The records that one resource produced, grouped by the scope that produced them.
/// <c>opentelemetry.proto.logs.v1.ResourceLogs</c>.
/// </summary>
public sealed class ResourceLogs
{
    /// <summary>Field 1. Absent when the resource is unknown.</summary>
    public Resource? Resource { get; set; }

    /// <summary>Field 2.</summary>
    public List<ScopeLogs> ScopeLogs { get; } = [];

    /// <summary>
    /// Field 3. Identifies the schema the resource attributes follow. Applies to
    /// <see cref="Resource"/> only, since each <see cref="ScopeLogs"/> carries its own.
    /// </summary>
    public string SchemaUrl { get; set; } = "";
}

/// <summary>
/// The records that one instrumentation scope produced.
/// <c>opentelemetry.proto.logs.v1.ScopeLogs</c>.
/// </summary>
public sealed class ScopeLogs
{
    /// <summary>Field 1. Absent means an unknown scope, equivalent to one with an empty name.</summary>
    public InstrumentationScope? Scope { get; set; }

    /// <summary>Field 2.</summary>
    public List<LogRecord> LogRecords { get; } = [];

    /// <summary>Field 3. Applies to the scope and to every record in this message.</summary>
    public string SchemaUrl { get; set; } = "";
}
