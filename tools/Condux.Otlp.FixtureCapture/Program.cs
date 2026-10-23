using Condux.Otlp.FixtureCapture;

// Captures the test fixtures. Run it through tools/capture-fixtures.sh, which starts the collector this
// tool exports through. The recorder answers on one port and files each export by its producer, the first
// path segment, and its signal, the last.
var fixtureDirectory = args.ElementAtOrDefault(0)
    ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "Condux.Otlp.Tests", "Fixtures");

// The collector's OTLP/HTTP receiver. Each signal has its own path under it.
var collector = new Uri(Environment.GetEnvironmentVariable("OTLP_FIXTURE_COLLECTOR") ?? "http://127.0.0.1:4318/");

// The recorder accepts on every interface, not just loopback, because one of its clients is the
// collector running in a container: that traffic arrives on the host's bridge address, and a
// loopback-only listener refuses it. The port is open for the seconds a capture run takes.
const string RecorderPrefix = "http://+:5399/";
const string RecorderAddress = "http://127.0.0.1:5399/";

var fixturesByRoute = new Dictionary<string, string>
{
    ["sdk/logs"] = "dotnet-sdk-logs.protobuf.bin",
    ["proto/logs"] = "collector-logs.protobuf.bin",
    ["json/logs"] = "collector-logs.json",
    ["js/logs"] = "js-sdk-logs.json",
    ["sdk/traces"] = "dotnet-sdk-traces.protobuf.bin",
    ["proto/traces"] = "collector-traces.protobuf.bin",
    ["json/traces"] = "collector-traces.json",
    ["js/traces"] = "js-sdk-traces.json",
};

using var recorder = new FixtureRecorder(RecorderPrefix, Path.GetFullPath(fixtureDirectory), fixturesByRoute);
recorder.Start();
Console.WriteLine($"recording to {Path.GetFullPath(fixtureDirectory)}");

LogEmitter.Emit([new Uri($"{RecorderAddress}sdk/v1/logs"), new Uri(collector, "v1/logs")]);
TraceEmitter.Emit([new Uri($"{RecorderAddress}sdk/v1/traces"), new Uri(collector, "v1/traces")]);
JsEmitter.Emit("emit.mjs", $"{RecorderAddress}js/v1/logs");
JsEmitter.Emit("emit-traces.mjs", $"{RecorderAddress}js/v1/traces");

if (await recorder.WaitForAllAsync(TimeSpan.FromSeconds(30)))
{
    Console.WriteLine("captured every fixture");
    return 0;
}

Console.Error.WriteLine($"missing: {string.Join(", ", recorder.Missing())}");
return 1;
