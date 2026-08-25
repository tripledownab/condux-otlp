namespace Condux.Otlp;

/// <summary>
/// How severe a record is, on the log data model's normalized scale.
/// <c>opentelemetry.proto.logs.v1.SeverityNumber</c>.
/// </summary>
/// <remarks>
/// The scale is ordered, and each named level owns four consecutive numbers so a source can preserve its
/// own finer gradations inside a level. Comparison therefore works directly: everything at
/// <see cref="Error"/> or above is an error or worse.
/// <para>
/// A decoder may return a number this enum does not name. The protocol reserves the range for future
/// levels, so treat an unnamed value by its position on the scale rather than rejecting it.
/// </para>
/// </remarks>
public enum SeverityNumber
{
    /// <summary>0. The record carries no severity.</summary>
    Unspecified = 0,

    /// <summary>The first of the four trace severities.</summary>
    Trace = 1,

    /// <summary>The second of the four trace severities.</summary>
    Trace2 = 2,

    /// <summary>The third of the four trace severities.</summary>
    Trace3 = 3,

    /// <summary>The fourth of the four trace severities.</summary>
    Trace4 = 4,

    /// <summary>The first of the four debug severities.</summary>
    Debug = 5,

    /// <summary>The second of the four debug severities.</summary>
    Debug2 = 6,

    /// <summary>The third of the four debug severities.</summary>
    Debug3 = 7,

    /// <summary>The fourth of the four debug severities.</summary>
    Debug4 = 8,

    /// <summary>The first of the four informational severities.</summary>
    Info = 9,

    /// <summary>The second of the four informational severities.</summary>
    Info2 = 10,

    /// <summary>The third of the four informational severities.</summary>
    Info3 = 11,

    /// <summary>The fourth of the four informational severities.</summary>
    Info4 = 12,

    /// <summary>The first of the four warning severities.</summary>
    Warn = 13,

    /// <summary>The second of the four warning severities.</summary>
    Warn2 = 14,

    /// <summary>The third of the four warning severities.</summary>
    Warn3 = 15,

    /// <summary>The fourth of the four warning severities.</summary>
    Warn4 = 16,

    /// <summary>The first of the four error severities.</summary>
    Error = 17,

    /// <summary>The second of the four error severities.</summary>
    Error2 = 18,

    /// <summary>The third of the four error severities.</summary>
    Error3 = 19,

    /// <summary>The fourth of the four error severities.</summary>
    Error4 = 20,

    /// <summary>The first of the four fatal severities.</summary>
    Fatal = 21,

    /// <summary>The second of the four fatal severities.</summary>
    Fatal2 = 22,

    /// <summary>The third of the four fatal severities.</summary>
    Fatal3 = 23,

    /// <summary>The fourth of the four fatal severities.</summary>
    Fatal4 = 24,
}
