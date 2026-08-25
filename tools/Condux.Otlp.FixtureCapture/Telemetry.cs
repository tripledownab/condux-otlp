using System.Diagnostics;

namespace Condux.Otlp.FixtureCapture;

/// <summary>
/// The activity source the emitter starts a span from, with a listener that samples everything.
/// </summary>
/// <remarks>
/// Without a listener that samples, <c>StartActivity</c> returns null and the records carry no trace or
/// span id, which would quietly cost the fixture the one field the encodings disagree about.
/// </remarks>
internal static class Telemetry
{
    internal static readonly ActivitySource Source = new("Condux.Otlp.FixtureCapture");

    static Telemetry()
    {
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = source => source == Source,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        });
    }
}
