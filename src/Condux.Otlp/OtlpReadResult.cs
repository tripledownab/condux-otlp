using System.Diagnostics.CodeAnalysis;

namespace Condux.Otlp;

/// <summary>
/// What a parse produced: the export, or the reason there is none.
/// </summary>
/// <remarks>
/// Parsing hostile input is an expected part of running a receiver, not an exceptional one, so a payload
/// that does not decode comes back as a value rather than a thrown exception.
/// </remarks>
public sealed class OtlpReadResult
{
    private OtlpReadResult(ExportLogsServiceRequest? value, string? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>The decoded export, or null when the parse failed.</summary>
    public ExportLogsServiceRequest? Value { get; }

    /// <summary>
    /// Why the parse failed, or null when it succeeded. The text says what was wrong with the shape of
    /// the payload and never quotes the payload itself, so it is safe to log.
    /// </summary>
    public string? Error { get; }

    /// <summary>Whether the payload decoded.</summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Value is not null;

    internal static OtlpReadResult Ok(ExportLogsServiceRequest value) => new(value, null);

    internal static OtlpReadResult Failed(string error) => new(null, error);
}
