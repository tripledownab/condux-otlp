using System.Globalization;
using System.Text.Json;

namespace Condux.Otlp.Json;

/// <summary>
/// Turns one JSON value into the CLR value the wire format says it carries.
/// </summary>
/// <remarks>
/// Two encoding rules drive most of this. Sixty-four bit integers are written as decimal strings because
/// JSON numbers cannot hold them exactly, and both a string and a number are accepted when reading. Trace
/// and span ids are hex rather than the base64 the standard protobuf JSON mapping uses for bytes, which
/// is the deviation that catches out anything transcoding OTLP through a generic protobuf library.
/// </remarks>
internal static class JsonScalars
{
    // Every number on the wire is culture-independent, so parsing must be too. A machine reading
    // a decimal point under a comma locale is the classic way this goes wrong.
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    internal static bool TryString(JsonElement element, out string value)
    {
        value = "";
        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = ToText(element);
        return true;
    }

    internal static bool TryBool(JsonElement element, out bool value)
    {
        value = element.ValueKind == JsonValueKind.True;
        return element.ValueKind is JsonValueKind.True or JsonValueKind.False;
    }

    internal static bool TryUInt64(JsonElement element, out ulong value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetUInt64(out value),
            JsonValueKind.String => ulong.TryParse(ToText(element), NumberStyles.None, Invariant, out value),
            _ => false,
        };
    }

    internal static bool TryInt64(JsonElement element, out long value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out value),
            JsonValueKind.String => long.TryParse(ToText(element), NumberStyles.Integer, Invariant, out value),
            _ => false,
        };
    }

    internal static bool TryUInt32(JsonElement element, out uint value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetUInt32(out value),
            JsonValueKind.String => uint.TryParse(ToText(element), NumberStyles.None, Invariant, out value),
            _ => false,
        };
    }

    internal static bool TryInt32(JsonElement element, out int value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out value),
            JsonValueKind.String => int.TryParse(ToText(element), NumberStyles.Integer, Invariant, out value),
            _ => false,
        };
    }

    internal static bool TryDouble(JsonElement element, out double value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDouble(out value),
            // JSON has no literal for NaN or either infinity, so the mapping carries those three as the
            // strings the invariant culture parses them from.
            JsonValueKind.String => double.TryParse(ToText(element), NumberStyles.Float, Invariant, out value),
            _ => false,
        };
    }

    internal static bool TryBase64Bytes(JsonElement element, out byte[] value)
    {
        value = [];
        if (element.ValueKind != JsonValueKind.String || !element.TryGetBytesFromBase64(out var decoded))
        {
            return false;
        }

        value = decoded!;
        return true;
    }

    /// <summary>
    /// Decodes a case-insensitive hex string, which is how trace and span ids travel. An empty string
    /// decodes to no bytes, which is how a record says it belongs to no trace.
    /// </summary>
    internal static bool TryHexBytes(JsonElement element, out byte[] value)
    {
        value = [];
        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var text = ToText(element).AsSpan();
        if (text.Length == 0)
        {
            return true;
        }

        if (text.Length % 2 != 0)
        {
            return false;
        }

        var bytes = new byte[text.Length / 2];
        for (var index = 0; index < bytes.Length; index++)
        {
            if (!TryNibble(text[index * 2], out var high) || !TryNibble(text[(index * 2) + 1], out var low))
            {
                return false;
            }

            bytes[index] = (byte)((high << 4) | low);
        }

        value = bytes;
        return true;
    }

    /// <summary>
    /// Reads a string element's text, which every read of a string goes through. Null comes back only
    /// for a JSON null, and every caller has already checked the element is a string. The parser does not
    /// validate a string's contents, so text that is not valid is found here, and is reported as such
    /// rather than as the <see cref="InvalidOperationException"/> the runtime throws. The filter matters:
    /// the runtime throws the same exception for a value of the wrong kind, which would be a missing check
    /// in a caller, and that must surface as the bug it is.
    /// </summary>
    private static string ToText(JsonElement element)
    {
        try
        {
            return element.GetString()!;
        }
        catch (InvalidOperationException invalid) when (element.ValueKind == JsonValueKind.String)
        {
            throw new InvalidJsonTextException(invalid);
        }
    }

    private static bool TryNibble(char character, out int value)
    {
        value = character switch
        {
            >= '0' and <= '9' => character - '0',
            >= 'a' and <= 'f' => character - 'a' + 10,
            >= 'A' and <= 'F' => character - 'A' + 10,
            _ => -1,
        };

        return value >= 0;
    }
}
