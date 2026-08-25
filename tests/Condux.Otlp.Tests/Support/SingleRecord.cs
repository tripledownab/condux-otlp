using Xunit;

namespace Condux.Otlp.Tests.Support;

/// <summary>
/// Wraps one log record in the messages that carry it, and reads it back out, so a test that is about a
/// record does not restate the three levels above it in either encoding.
/// </summary>
internal static class SingleRecord
{
    private const int ResourceLogsField = 1;
    private const int ScopeLogsField = 2;
    private const int LogRecordsField = 2;

    /// <summary>The protobuf payload for an export carrying the one record given.</summary>
    internal static byte[] Wrap(byte[] logRecord)
        => Wire.Bytes(ResourceLogsField, Wire.Bytes(ScopeLogsField, Wire.Bytes(LogRecordsField, logRecord)));

    /// <summary>Parses a JSON export carrying the one record given, without asserting the outcome.</summary>
    internal static OtlpReadResult ParseJson(string logRecord)
        => OtlpLogs.ParseJson($$"""
            {"resourceLogs":[{"scopeLogs":[{"logRecords":[{{logRecord}}]}]}]}
            """);

    /// <summary>The record from a JSON export that is expected to decode.</summary>
    internal static LogRecord DecodeJson(string logRecord)
    {
        var result = ParseJson(logRecord);
        Assert.True(result.IsSuccess, result.Error);
        return From(result.Value);
    }

    internal static LogRecord From(ExportLogsServiceRequest request)
        => request.ResourceLogs[0].ScopeLogs[0].LogRecords[0];

    internal static AnyValue Attribute(LogRecord record, string key)
        => record.Attributes.Single(pair => pair.Key == key).Value!;
}
