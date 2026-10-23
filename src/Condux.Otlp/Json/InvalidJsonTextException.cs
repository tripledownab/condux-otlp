namespace Condux.Otlp.Json;

/// <summary>
/// Raised when a JSON string, or a member name compared against one, does not hold valid text: invalid
/// UTF-8, or an escape for an unpaired surrogate.
/// </summary>
/// <remarks>
/// The parser accepts such a document and the runtime throws <see cref="InvalidOperationException"/>
/// only when the text is read. That exception also means a value was read as the wrong kind, which is a
/// decoder bug, so it is translated into this one at the two places text is read, and the entry point
/// catches only this. A broken kind check therefore still surfaces in the tests rather than being
/// reported as bad text.
/// </remarks>
internal sealed class InvalidJsonTextException(InvalidOperationException inner)
    : Exception("a JSON string does not hold valid text", inner);
