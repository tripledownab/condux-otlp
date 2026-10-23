using System.Diagnostics;

namespace Condux.Otlp.FixtureCapture;

/// <summary>
/// Runs a JavaScript emitter beside this one, one script per signal, so the fixtures carry the JSON
/// encoding as written by an implementation that shares no code with the collector's.
/// </summary>
/// <remarks>
/// Its dependencies are installed before each run. A failure is reported and not thrown, because the
/// recorder reports what is missing at the end of a run and one producer failing should not hide which.
/// </remarks>
internal static class JsEmitter
{
    internal static void Emit(string script, string endpoint)
    {
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "js-emitter"));

        // Run on every call rather than only when node_modules is missing, since a checkout that installed
        // before a dependency was added would otherwise run without it. It is quick when nothing changed.
        Run(directory, "npm", "install --silent --no-audit --no-fund");
        Run(directory, "node", $"{script} {endpoint}");
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
