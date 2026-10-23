# Where these fixtures came from

Captured by `tools/capture-fixtures.sh` on 2026-10-23. Nothing here was produced by this
repository's own encoder, which is the point: a decoder tested against its own writer shares its
assumptions and cannot disprove them.

| Fixture | Producer |
|---|---|
| `dotnet-sdk-logs.protobuf.bin` | OpenTelemetry .NET exporter 1.18.0, straight to the recorder |
| `collector-logs.protobuf.bin` | otelcol-contrib version 0.142.0, `otlphttp` encoding `proto` |
| `collector-logs.json` | the same collector and the same records, `otlphttp` encoding `json` |
| `js-sdk-logs.json` | OpenTelemetry JS SDK 0.221.0, whose HTTP exporter writes JSON directly |
| `dotnet-sdk-traces.protobuf.bin` | OpenTelemetry .NET exporter 1.18.0, straight to the recorder |
| `collector-traces.protobuf.bin` | otelcol-contrib version 0.142.0, `otlphttp` encoding `proto` |
| `collector-traces.json` | the same collector and the same spans, `otlphttp` encoding `json` |
| `js-sdk-traces.json` | OpenTelemetry JS SDK 2.10.0, whose HTTP exporter writes JSON directly |

Each pair of collector fixtures is one export seen in both encodings. They decode to equal messages, and
the tests that assert it are what pin the hex id rule that the encodings disagree about. For traces that
rule covers the parent span id and a link's ids as well as the trace and span id.

The JS fixtures are separate exports from an implementation that shares no code with the collector. Where
the two JSON producers agree, on lowerCamelCase member names and integer enum values, the agreement is
the encoding rather than one implementation's habit, which is what the decoder's strictness rests on.

Every payload here is the export itself. Compression belongs to the transport and the producers differ
on it, the collector gzipping where the SDK exporters do not, so the recorder undoes it before writing.
The stack traces the trace fixtures carry have their source paths rewritten by the producers, so a
capture is identical wherever it runs.
