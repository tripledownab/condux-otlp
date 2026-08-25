using Condux.Otlp.FixtureCapture;

// Captures the test fixtures. Run it through tools/capture-fixtures.sh, which starts the collector this
// tool exports through. The recorder answers on one port and files each export by the first path segment.
var fixtureDirectory = args.ElementAtOrDefault(0)
    ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "Condux.Otlp.Tests", "Fixtures");

var collector = new Uri(Environment.GetEnvironmentVariable("OTLP_FIXTURE_COLLECTOR") ?? "http://127.0.0.1:4318/v1/logs");

// The recorder accepts on every interface, not just loopback, because one of its clients is the
// collector running in a container: that traffic arrives on the host's bridge address, and a
// loopback-only listener refuses it. The port is open for the seconds a capture run takes.
const string RecorderPrefix = "http://+:5399/";
const string RecorderAddress = "http://127.0.0.1:5399/";

var fixturesBySegment = new Dictionary<string, string>
{
    ["sdk"] = "dotnet-sdk-logs.protobuf.bin",
    ["proto"] = "collector-logs.protobuf.bin",
    ["json"] = "collector-logs.json",
    ["js"] = "js-sdk-logs.json",
};

using var recorder = new FixtureRecorder(RecorderPrefix, Path.GetFullPath(fixtureDirectory), fixturesBySegment);
recorder.Start();
Console.WriteLine($"recording to {Path.GetFullPath(fixtureDirectory)}");

LogEmitter.Emit([new Uri($"{RecorderAddress}sdk/v1/logs"), collector]);
JsEmitter.Emit($"{RecorderAddress}js/v1/logs");

if (await recorder.WaitForAllAsync(TimeSpan.FromSeconds(30)))
{
    Console.WriteLine("captured every fixture");
    return 0;
}

Console.Error.WriteLine($"missing: {string.Join(", ", recorder.Missing())}");
return 1;
