#!/usr/bin/env bash
# Recaptures the test fixtures from real OpenTelemetry software.
#
# The fixtures are committed, so this only needs running when they should change: a new protocol version,
# a new field worth covering, or a doubt about what a real exporter emits. It writes the version of every
# producer into the fixtures' provenance file, because a fixture nobody can trace is a fixture nobody can
# trust.
#
# Needs Docker, and Node for the JavaScript producer. Uses ports 4318 and 5399.
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(dirname "$here")"
fixtures="$root/tests/Condux.Otlp.Tests/Fixtures"
container="condux-otlp-fixture-collector"
image="${COLLECTOR_IMAGE:-otel/opentelemetry-collector-contrib:0.142.0}"

cleanup() { docker rm -f "$container" >/dev/null 2>&1 || true; }
trap cleanup EXIT
cleanup

echo "starting $image"
# The collector reaches the recorder on the host as host.docker.internal. Docker Desktop defines that name
# by itself; Docker Engine on Linux does not, and the collector's exports then fail with no fixture
# written, so the name is mapped explicitly.
docker run -d --name "$container" \
  --add-host=host.docker.internal:host-gateway \
  -p 127.0.0.1:4318:4318 \
  -v "$here/collector-config.yaml:/etc/otelcol-contrib/config.yaml:ro" \
  "$image" >/dev/null

# The collector opens its receiver a moment after the container starts, and an export that arrives first
# is simply refused, so wait for the port rather than sleeping a guessed interval.
for _ in $(seq 1 50); do
  if nc -z 127.0.0.1 4318 2>/dev/null; then break; fi
  sleep 0.2
done
nc -z 127.0.0.1 4318 2>/dev/null || { echo "the collector never opened port 4318"; docker logs "$container"; exit 1; }

dotnet run --project "$here/Condux.Otlp.FixtureCapture" -- "$fixtures"

collector_version="$(docker exec "$container" /otelcol-contrib --version 2>/dev/null | head -1 || echo "$image")"
exporter_version="$(grep -o 'OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="[^"]*"' \
  "$here/Condux.Otlp.FixtureCapture/Condux.Otlp.FixtureCapture.csproj" | cut -d'"' -f3)"
js_version="$(grep -o '"@opentelemetry/sdk-logs": "[^"]*"' "$here/js-emitter/package.json" | cut -d'"' -f4)"
js_trace_version="$(grep -o '"@opentelemetry/sdk-trace-base": "[^"]*"' "$here/js-emitter/package.json" | cut -d'"' -f4)"

cat > "$fixtures/PROVENANCE.md" <<EOF
# Where these fixtures came from

Captured by \`tools/capture-fixtures.sh\` on $(date -u +%Y-%m-%d). Nothing here was produced by this
repository's own encoder, which is the point: a decoder tested against its own writer shares its
assumptions and cannot disprove them.

| Fixture | Producer |
|---|---|
| \`dotnet-sdk-logs.protobuf.bin\` | OpenTelemetry .NET exporter $exporter_version, straight to the recorder |
| \`collector-logs.protobuf.bin\` | $collector_version, \`otlphttp\` encoding \`proto\` |
| \`collector-logs.json\` | the same collector and the same records, \`otlphttp\` encoding \`json\` |
| \`js-sdk-logs.json\` | OpenTelemetry JS SDK $js_version, whose HTTP exporter writes JSON directly |
| \`dotnet-sdk-traces.protobuf.bin\` | OpenTelemetry .NET exporter $exporter_version, straight to the recorder |
| \`collector-traces.protobuf.bin\` | $collector_version, \`otlphttp\` encoding \`proto\` |
| \`collector-traces.json\` | the same collector and the same spans, \`otlphttp\` encoding \`json\` |
| \`js-sdk-traces.json\` | OpenTelemetry JS SDK $js_trace_version, whose HTTP exporter writes JSON directly |

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
EOF

echo "wrote $fixtures/PROVENANCE.md"
ls -l "$fixtures"
