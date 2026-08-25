namespace Condux.Otlp;

/// <summary>
/// What produced the telemetry: the service, host, container and so on, as attributes.
/// <c>opentelemetry.proto.resource.v1.Resource</c>.
/// </summary>
public sealed class Resource
{
    /// <summary>Field 1. Keys are unique in a well-formed message.</summary>
    public List<KeyValue> Attributes { get; } = [];

    /// <summary>Field 2. Zero means nothing was dropped.</summary>
    public uint DroppedAttributesCount { get; set; }
}

/// <summary>
/// The library that produced a batch of records: its name, version and attributes.
/// <c>opentelemetry.proto.common.v1.InstrumentationScope</c>.
/// </summary>
public sealed class InstrumentationScope
{
    /// <summary>Field 1. Empty means the name is unknown.</summary>
    public string Name { get; set; } = "";

    /// <summary>Field 2. Empty means the version is unknown.</summary>
    public string Version { get; set; } = "";

    /// <summary>Field 3.</summary>
    public List<KeyValue> Attributes { get; } = [];

    /// <summary>Field 4. Zero means nothing was dropped.</summary>
    public uint DroppedAttributesCount { get; set; }
}
