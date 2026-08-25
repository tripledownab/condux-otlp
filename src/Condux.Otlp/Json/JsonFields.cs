using System.Text.Json;

namespace Condux.Otlp.Json;

/// <summary>Decodes a whole message out of one JSON object.</summary>
internal delegate bool JsonMessageReader<TMessage>(JsonElement element, out TMessage message);

/// <summary>
/// Reads named members off a JSON object.
/// </summary>
/// <remarks>
/// Every method here treats an absent member as success carrying the type's default, because that is
/// what the encoding means by leaving a field out. Only a member that is present and does not decode
/// returns false. Member names are lowerCamelCase, the only spelling the encoding allows.
/// </remarks>
internal static class JsonFields
{
    /// <summary>Finds a member that carries a value. JSON null means the default, so it counts as absent.</summary>
    internal static bool TryGet(JsonElement parent, string name, out JsonElement value)
        => parent.TryGetProperty(name, out value) && value.ValueKind != JsonValueKind.Null;

    internal static bool TryString(JsonElement parent, string name, out string value)
    {
        value = "";
        return !TryGet(parent, name, out var element) || JsonScalars.TryString(element, out value);
    }

    internal static bool TryUInt64(JsonElement parent, string name, out ulong value)
    {
        value = 0;
        return !TryGet(parent, name, out var element) || JsonScalars.TryUInt64(element, out value);
    }

    internal static bool TryUInt32(JsonElement parent, string name, out uint value)
    {
        value = 0;
        return !TryGet(parent, name, out var element) || JsonScalars.TryUInt32(element, out value);
    }

    internal static bool TryInt32(JsonElement parent, string name, out int value)
    {
        value = 0;
        return !TryGet(parent, name, out var element) || JsonScalars.TryInt32(element, out value);
    }

    internal static bool TryHexBytes(JsonElement parent, string name, out byte[] value)
    {
        value = [];
        return !TryGet(parent, name, out var element) || JsonScalars.TryHexBytes(element, out value);
    }

    /// <summary>Reads a nested message member. Absent leaves <paramref name="message"/> null.</summary>
    internal static bool TryNested<TMessage>(
        JsonElement parent,
        string name,
        JsonMessageReader<TMessage> readMessage,
        out TMessage? message)
        where TMessage : class
    {
        message = null;
        if (!TryGet(parent, name, out var element))
        {
            return true;
        }

        if (!readMessage(element, out var decoded))
        {
            return false;
        }

        message = decoded;
        return true;
    }

    /// <summary>Reads a repeated message member into <paramref name="into"/>. Absent adds nothing.</summary>
    internal static bool TryList<TItem>(
        JsonElement parent,
        string name,
        List<TItem> into,
        JsonMessageReader<TItem> readItem)
    {
        if (!TryGet(parent, name, out var array))
        {
            return true;
        }

        if (array.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var element in array.EnumerateArray())
        {
            if (!readItem(element, out var item))
            {
                return false;
            }

            into.Add(item);
        }

        return true;
    }
}
