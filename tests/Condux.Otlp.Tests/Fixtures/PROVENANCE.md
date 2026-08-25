# Where these fixtures came from

Captured by `tools/capture-fixtures.sh` on 2026-08-24. Nothing here was produced by this
repository's own encoder, which is the point: a decoder tested against its own writer shares its
assumptions and cannot disprove them.

| Fixture | Producer |
|---|---|
| `dotnet-sdk-logs.protobuf.bin` | OpenTelemetry .NET exporter 1.18.0, straight to the recorder |
| `collector-logs.protobuf.bin` | otelcol-contrib version 0.142.0, `otlphttp` encoding `proto` |
| `collector-logs.json` | the same collector and the same records, `otlphttp` encoding `json` |
| `js-sdk-logs.json` | OpenTelemetry JS SDK 0.221.0, whose HTTP exporter writes JSON directly |

The two collector fixtures are one export seen in both encodings. They decode to equal messages, and the
test that asserts it is what pins the hex trace id rule that the encodings disagree about.

The JS fixture is a separate export from an implementation that shares no code with the collector. Where
the two JSON producers agree, on lowerCamelCase member names and integer enum values, the agreement is
the encoding rather than one implementation's habit, which is what the decoder's strictness rests on.

Every payload here is the export itself. Compression belongs to the transport and the producers differ
on it, the collector gzipping where the two SDK exporters do not, so the recorder undoes it before
writing.
