using System.Diagnostics;

namespace Condux.Otlp.FixtureCapture;

/// <summary>
/// Runs the JavaScript emitter beside this one, so the fixtures carry the JSON encoding as written by an
/// implementation that shares no code with the collector's.
/// </summary>
/// <remarks>
/// Its dependencies are installed on first use. A failure is reported and not thrown, because the
/// recorder reports what is missing at the end of a run and one producer failing should not hide which.
/// </remarks>
internal static class JsEmitter
{
    internal static void Emit(string endpoint)
    {
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "js-emitter"));
        if (!Directory.Exists(Path.Combine(directory, "node_modules")))
        {
            Run(directory, "npm", "install --silent --no-audit --no-fund");
        }

        Run(directory, "node", $"emit.mjs {endpoint}");
    }

    private static void Run(string directory, string command, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(command, arguments)
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
        });

        if (process is null)
        {
            Console.Error.WriteLine($"could not start {command}");
            return;
        }

        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine($"{command} {arguments} exited with {process.ExitCode}");
        }
    }
}
