namespace Condux.Otlp;

/// <summary>Which member of <see cref="AnyValue"/>'s <c>value</c> oneof is set.</summary>
public enum AnyValueKind
{
    /// <summary>No member is set. The protocol calls this an empty value and it is valid.</summary>
    None = 0,

    /// <summary><see cref="AnyValue.StringValue"/>.</summary>
    String = 1,

    /// <summary><see cref="AnyValue.BoolValue"/>.</summary>
    Bool = 2,

    /// <summary><see cref="AnyValue.IntValue"/>.</summary>
    Int = 3,

    /// <summary><see cref="AnyValue.DoubleValue"/>.</summary>
    Double = 4,

    /// <summary><see cref="AnyValue.ArrayValue"/>.</summary>
    Array = 5,

    /// <summary><see cref="AnyValue.KvlistValue"/>.</summary>
    Kvlist = 6,

    /// <summary><see cref="AnyValue.BytesValue"/>.</summary>
    Bytes = 7,
}

/// <summary>
/// A value of any of the protocol's types: a scalar, an array, or a list of key-value pairs.
/// <c>opentelemetry.proto.common.v1.AnyValue</c>.
/// </summary>
/// <remarks>
/// The protocol declares these members as a <c>oneof</c>, so at most one is set. <see cref="Kind"/> says
/// which. Setting one member does not clear the others, so read through <see cref="Kind"/> rather than
/// testing members for their default value: an <see cref="IntValue"/> of zero and an unset field look
/// the same otherwise. How a value should be shown is the caller's decision, so this type carries the
/// data and no formatting.
/// </remarks>
public sealed class AnyValue
{
    /// <summary>Which member is set.</summary>
    public AnyValueKind Kind { get; set; }

    /// <summary>Field 1. Meaningful when <see cref="Kind"/> is <see cref="AnyValueKind.String"/>.</summary>
    public string? StringValue { get; set; }

    /// <summary>Field 2. Meaningful when <see cref="Kind"/> is <see cref="AnyValueKind.Bool"/>.</summary>
    public bool BoolValue { get; set; }

    /// <summary>Field 3. Meaningful when <see cref="Kind"/> is <see cref="AnyValueKind.Int"/>.</summary>
    public long IntValue { get; set; }

    /// <summary>Field 4. Meaningful when <see cref="Kind"/> is <see cref="AnyValueKind.Double"/>.</summary>
    public double DoubleValue { get; set; }

    /// <summary>Field 5. Meaningful when <see cref="Kind"/> is <see cref="AnyValueKind.Array"/>.</summary>
    public ArrayValue? ArrayValue { get; set; }

    /// <summary>Field 6. Meaningful when <see cref="Kind"/> is <see cref="AnyValueKind.Kvlist"/>.</summary>
    public KeyValueList? KvlistValue { get; set; }

    /// <summary>Field 7. Meaningful when <see cref="Kind"/> is <see cref="AnyValueKind.Bytes"/>.</summary>
    public byte[]? BytesValue { get; set; }
}

/// <summary>
/// An ordered list of values. <c>opentelemetry.proto.common.v1.ArrayValue</c>. The protocol needs this
/// wrapper because a <c>oneof</c> member cannot itself be repeated.
/// </summary>
public sealed class ArrayValue
{
    /// <summary>Field 1. May be empty.</summary>
    public List<AnyValue> Values { get; } = [];
}

/// <summary>
/// A list of key-value pairs. <c>opentelemetry.proto.common.v1.KeyValueList</c>. Keys are unique in a
/// well-formed message, but nothing in the encoding enforces it, so a decoded list may repeat a key.
/// </summary>
public sealed class KeyValueList
{
    /// <summary>Field 1. May be empty.</summary>
    public List<KeyValue> Values { get; } = [];
}

/// <summary>A key-value pair. <c>opentelemetry.proto.common.v1.KeyValue</c>.</summary>
public sealed class KeyValue
{
    /// <summary>Field 1.</summary>
    public string Key { get; set; } = "";

    /// <summary>Field 2. Absent when the pair carries no value.</summary>
    public AnyValue? Value { get; set; }
}
