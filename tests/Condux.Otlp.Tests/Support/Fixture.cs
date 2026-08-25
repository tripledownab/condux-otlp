namespace Condux.Otlp.Tests.Support;

/// <summary>
/// The exports captured from real OpenTelemetry software. See <c>Fixtures/PROVENANCE.md</c> for what
/// produced each one and <c>tools/capture-fixtures.sh</c> for how to capture them again.
/// </summary>
internal static class Fixture
{
    /// <summary>One export straight from the OpenTelemetry .NET SDK's own exporter.</summary>
    internal const string DotnetSdkProtobuf = "dotnet-sdk-logs.protobuf.bin";

    /// <summary>The same records, relayed by the OpenTelemetry Collector as protobuf.</summary>
    internal const string CollectorProtobuf = "collector-logs.protobuf.bin";

    /// <summary>The same records again, relayed by the same collector as JSON.</summary>
    internal const string CollectorJson = "collector-logs.json";

    /// <summary>
    /// A separate export written by the OpenTelemetry JS SDK, whose HTTP exporter produces JSON with no
    /// collector in between. It shares no code with the collector, so where the two agree the agreement
    /// is the encoding rather than one implementation's habit.
    /// </summary>
    internal const string JsSdkJson = "js-sdk-logs.json";

    internal static byte[] Bytes(string name)
        => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}
