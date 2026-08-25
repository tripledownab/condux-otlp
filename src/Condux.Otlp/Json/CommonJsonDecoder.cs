using System.Text.Json;

namespace Condux.Otlp.Json;

/// <summary>Decodes the common and resource messages from the JSON encoding.</summary>
internal static class CommonJsonDecoder
{
    internal static bool TryReadAnyValue(JsonElement element, out AnyValue value)
    {
        value = new AnyValue();
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var (name, readMember) in OneofMembers)
        {
            if (JsonFields.TryGet(element, name, out var member))
            {
                return readMember(member, value);
            }
        }

        // No member present is the empty value, which the protocol allows.
        return true;
    }

    internal static bool TryReadKeyValue(JsonElement element, out KeyValue pair)
    {
        pair = new KeyValue();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryString(element, "key", out var key)
            || !JsonFields.TryNested(element, "value", TryReadAnyValue, out AnyValue? value))
        {
            return false;
        }

        pair.Key = key;
        pair.Value = value;
        return true;
    }

    internal static bool TryReadResource(JsonElement element, out Resource resource)
    {
        resource = new Resource();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryList(element, "attributes", resource.Attributes, TryReadKeyValue)
            || !JsonFields.TryUInt32(element, "droppedAttributesCount", out var dropped))
        {
            return false;
        }

        resource.DroppedAttributesCount = dropped;
        return true;
    }

    internal static bool TryReadInstrumentationScope(JsonElement element, out InstrumentationScope scope)
    {
        scope = new InstrumentationScope();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryString(element, "name", out var name)
            || !JsonFields.TryString(element, "version", out var version)
            || !JsonFields.TryList(element, "attributes", scope.Attributes, TryReadKeyValue)
            || !JsonFields.TryUInt32(element, "droppedAttributesCount", out var dropped))
        {
            return false;
        }

        scope.Name = name;
        scope.Version = version;
        scope.DroppedAttributesCount = dropped;
        return true;
    }

    /// <summary>
    /// The members of the value oneof, in field order. At most one appears on a well-formed value, so
    /// the first one present decides the kind.
    /// </summary>
    private static readonly (string Name, Func<JsonElement, AnyValue, bool> Read)[] OneofMembers =
    [
        ("stringValue", ReadString),
        ("boolValue", ReadBool),
        ("intValue", ReadInt),
        ("doubleValue", ReadDouble),
        ("arrayValue", ReadArray),
        ("kvlistValue", ReadKvlist),
        ("bytesValue", ReadBytes),
    ];

    private static bool ReadString(JsonElement element, AnyValue value)
    {
        if (!JsonScalars.TryString(element, out var text))
        {
            return false;
        }

        value.Kind = AnyValueKind.String;
        value.StringValue = text;
        return true;
    }

    private static bool ReadBool(JsonElement element, AnyValue value)
    {
        if (!JsonScalars.TryBool(element, out var flag))
        {
            return false;
        }

        value.Kind = AnyValueKind.Bool;
        value.BoolValue = flag;
        return true;
    }

    private static bool ReadInt(JsonElement element, AnyValue value)
    {
        if (!JsonScalars.TryInt64(element, out var number))
        {
            return false;
        }

        value.Kind = AnyValueKind.Int;
        value.IntValue = number;
        return true;
    }

    private static bool ReadDouble(JsonElement element, AnyValue value)
    {
        if (!JsonScalars.TryDouble(element, out var fraction))
        {
            return false;
        }

        value.Kind = AnyValueKind.Double;
        value.DoubleValue = fraction;
        return true;
    }

    private static bool ReadArray(JsonElement element, AnyValue value)
    {
        var array = new ArrayValue();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryList(element, "values", array.Values, TryReadAnyValue))
        {
            return false;
        }

        value.Kind = AnyValueKind.Array;
        value.ArrayValue = array;
        return true;
    }

    private static bool ReadKvlist(JsonElement element, AnyValue value)
    {
        var list = new KeyValueList();
        if (element.ValueKind != JsonValueKind.Object
            || !JsonFields.TryList(element, "values", list.Values, TryReadKeyValue))
        {
            return false;
        }

        value.Kind = AnyValueKind.Kvlist;
        value.KvlistValue = list;
        return true;
    }

    private static bool ReadBytes(JsonElement element, AnyValue value)
    {
        if (!JsonScalars.TryBase64Bytes(element, out var raw))
        {
            return false;
        }

        value.Kind = AnyValueKind.Bytes;
        value.BytesValue = raw;
        return true;
    }
}
