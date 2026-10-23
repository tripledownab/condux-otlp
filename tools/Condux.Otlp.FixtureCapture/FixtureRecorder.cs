using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;

namespace Condux.Otlp.FixtureCapture;

/// <summary>
/// Stands in for an OTLP receiver and writes each export it is sent to a file.
/// </summary>
/// <remarks>
/// The fixture is chosen by the request's route: its first path segment, which names the producer, and
/// its last, which names the signal (<c>logs</c> or <c>traces</c>). Keying on both is needed because the
/// collector sends both signals under one producer segment. Every export is answered with a success
/// response encoded by the library under test, which makes a capture run a live check that a real
/// exporter accepts what the library writes.
/// </remarks>
internal sealed class FixtureRecorder : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _directory;
    private readonly IReadOnlyDictionary<string, string> _fixturesByRoute;
    private readonly ConcurrentDictionary<string, string> _captured = new();

    internal FixtureRecorder(string prefix, string directory, IReadOnlyDictionary<string, string> fixturesByRoute)
    {
        _listener.Prefixes.Add(prefix);
        _directory = directory;
        _fixturesByRoute = fixturesByRoute;
    }

    internal void Start()
    {
        Directory.CreateDirectory(_directory);
        _listener.Start();
        _ = Task.Run(AcceptAsync);
    }

    internal async Task<bool> WaitForAllAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_captured.Count == _fixturesByRoute.Count)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        return false;
    }

    internal IEnumerable<string> Missing()
        => _fixturesByRoute.Values.Where(name => !_captured.ContainsKey(name));

    public void Dispose() => ((IDisposable)_listener).Dispose();

    private async Task AcceptAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            await HandleAsync(context);
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        using var body = new MemoryStream();
        await ReadPayloadAsync(context.Request, body);
        var segments = context.Request.Url?.Segments ?? [];
        var signal = (segments.LastOrDefault() ?? "").Trim('/');
        var route = $"{(segments.ElementAtOrDefault(1) ?? "").Trim('/')}/{signal}";

        if (_fixturesByRoute.TryGetValue(route, out var fixture))
        {
            var path = Path.Combine(_directory, fixture);
            await File.WriteAllBytesAsync(path, body.ToArray());
            _captured[fixture] = path;
            Console.WriteLine($"captured {fixture} ({body.Length} bytes, {context.Request.ContentType})");
        }
        else
        {
            Console.WriteLine($"ignored an export on an unmapped path: {context.Request.Url}");
        }

        await RespondAsync(context, signal);
    }

    /// <summary>
    /// Reads the export, undoing transport compression.
    /// </summary>
    /// <remarks>
    /// Compression is a property of the transport rather than of OTLP, and the producers here disagree
    /// about it: the collector gzips by default, the two SDK exporters do not. A fixture has to be the
    /// payload itself either way, so the compression comes off here. An encoding this method does not
    /// know is a hard failure, because writing the compressed bytes under a fixture's name would produce
    /// a file that looks captured and decodes to nothing.
    /// </remarks>
    private static async Task ReadPayloadAsync(HttpListenerRequest request, Stream destination)
    {
        var encoding = (request.Headers["Content-Encoding"] ?? "").Trim().ToLowerInvariant();
        switch (encoding)
        {
            case "":
            case "identity":
                await request.InputStream.CopyToAsync(destination);
                return;
            case "gzip":
                await using (var gzip = new GZipStream(request.InputStream, CompressionMode.Decompress))
                {
                    await gzip.CopyToAsync(destination);
                }

                return;
            default:
                throw new NotSupportedException($"an export arrived with Content-Encoding '{encoding}'");
        }
    }

    /// <summary>
    /// Answers with an empty success of the signal's own response type. The protocol requires the response
    /// to carry the same content type the request did, so the encoding is chosen from the request rather
    /// than fixed.
    /// </summary>
    private static async Task RespondAsync(HttpListenerContext context, string signal)
    {
        var isJson = (context.Request.ContentType ?? "").Contains("json", StringComparison.OrdinalIgnoreCase);
        var payload = (signal, isJson) switch
        {
            ("traces", true) => System.Text.Encoding.UTF8.GetBytes(new ExportTraceServiceResponse().ToJson()),
            ("traces", false) => new ExportTraceServiceResponse().ToProtobuf(),
            (_, true) => System.Text.Encoding.UTF8.GetBytes(new ExportLogsServiceResponse().ToJson()),
            (_, false) => new ExportLogsServiceResponse().ToProtobuf(),
        };

        context.Response.StatusCode = 200;
        context.Response.ContentType = isJson ? "application/json" : "application/x-protobuf";
        context.Response.ContentLength64 = payload.Length;
        await context.Response.OutputStream.WriteAsync(payload);
        context.Response.Close();
    }
}
