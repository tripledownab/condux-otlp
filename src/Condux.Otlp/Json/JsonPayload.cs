using System.Text.Json;

namespace Condux.Otlp.Json;

/// <summary>
/// Parses a JSON-encoded export body for any signal, so the two ways of handing one in, the depth bound
/// and the exceptions a hostile payload can raise are written once rather than once per signal.
/// </summary>
internal static class JsonPayload
{
    // What a failed result says. Like every failure message here, each names the shape that was wrong
    // and never quotes the payload.
    private const string NotJson = "the payload is not well-formed JSON, or nests deeper than the decoder allows";
    private const string NotText = "the payload contains a string that is not valid UTF-8 or UTF-16";

    /// <summary>
    /// Nesting is bounded at parse time rather than during decoding, because the decoder follows the
    /// document's own structure and so can go no deeper than the document does.
    /// </summary>
    private static readonly JsonDocumentOptions DocumentOptions = new() { MaxDepth = Limits.MaxDepth };

    internal static OtlpReadResult<TExport> Parse<TExport>(
        ReadOnlyMemory<byte> utf8Payload,
        JsonMessageReader<TExport> readExport,
        string notAnExport)
        where TExport : class
    {
        try
        {
            using var document = JsonDocument.Parse(utf8Payload, DocumentOptions);
            return Read(document, readExport, notAnExport);
        }
        catch (JsonException)
        {
            return OtlpReadResult<TExport>.Failed(NotJson);
        }
        catch (InvalidJsonTextException)
        {
            // JsonDocument.Parse does not validate what is inside a string. Reading one that holds invalid
            // UTF-8, or an escape for an unpaired surrogate, fails later, from inside the decoder.
            return OtlpReadResult<TExport>.Failed(NotText);
        }
    }

    internal static OtlpReadResult<TExport> Parse<TExport>(
        string payload,
        JsonMessageReader<TExport> readExport,
        string notAnExport)
        where TExport : class
    {
        ArgumentNullException.ThrowIfNull(payload);
        try
        {
            using var document = JsonDocument.Parse(payload, DocumentOptions);
            return Read(document, readExport, notAnExport);
        }
        catch (JsonException)
        {
            return OtlpReadResult<TExport>.Failed(NotJson);
        }
        catch (InvalidJsonTextException)
        {
            return OtlpReadResult<TExport>.Failed(NotText);
        }
        catch (ArgumentException)
        {
            // Text holding an unpaired surrogate cannot be transcoded to the UTF-8 the parser reads, and
            // the parser reports that as an argument error before any JSON is read. A null payload never
            // reaches here: that is the caller's bug, not hostile input, and it throws above.
            return OtlpReadResult<TExport>.Failed(NotText);
        }
    }

    private static OtlpReadResult<TExport> Read<TExport>(
        JsonDocument document,
        JsonMessageReader<TExport> readExport,
        string notAnExport)
        where TExport : class
        => readExport(document.RootElement, out var export)
            ? OtlpReadResult<TExport>.Ok(export)
            : OtlpReadResult<TExport>.Failed(notAnExport);
}
