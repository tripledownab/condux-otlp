namespace Condux.Otlp;

/// <summary>
/// The spans that one resource produced, grouped by the scope that produced them.
/// <c>opentelemetry.proto.trace.v1.ResourceSpans</c>.
/// </summary>
public sealed class ResourceSpans
{
    /// <summary>Field 1. Absent when the resource is unknown.</summary>
    public Resource? Resource { get; set; }

    /// <summary>Field 2.</summary>
    public List<ScopeSpans> ScopeSpans { get; } = [];

    /// <summary>
    /// Field 3. Identifies the schema the resource attributes follow. Applies to
    /// <see cref="Resource"/> only, since each <see cref="ScopeSpans"/> carries its own.
    /// </summary>
    public string SchemaUrl { get; set; } = "";
}

/// <summary>
/// The spans that one instrumentation scope produced.
/// <c>opentelemetry.proto.trace.v1.ScopeSpans</c>.
/// </summary>
public sealed class ScopeSpans
{
    /// <summary>Field 1. Absent means an unknown scope, equivalent to one with an empty name.</summary>
    public InstrumentationScope? Scope { get; set; }

    /// <summary>Field 2.</summary>
    public List<Span> Spans { get; } = [];

    /// <summary>Field 3. Applies to the scope and to every span in this message.</summary>
    public string SchemaUrl { get; set; } = "";
}
