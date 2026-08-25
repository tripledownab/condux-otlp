# Condux.Otlp

Read OpenTelemetry Protocol (OTLP) **logs** payloads in .NET, in both encodings the protocol defines, with
no package dependencies at all.

```
dotnet add package Condux.Otlp
```

Targets `net8.0`, `net9.0` and `net10.0`. Apache-2.0.

## Scope

| | |
|---|---|
| Signals | logs. Traces and metrics are not implemented |
| Encodings | binary protobuf and JSON, both read |
| Writes | the service response, which a receiver has to answer with |
| Version | `0.x`, so names and shapes may still change |
| Tested against | the OpenTelemetry .NET exporter, the OpenTelemetry Collector and the OpenTelemetry JS SDK |

Build and test from source with `dotnet test Condux.Otlp.slnx -c Release`. That runs the suite against
all three targets and so needs the .NET 8, 9 and 10 runtimes installed; add `-f net10.0` to run against
one.

## Why it exists

Every other major ecosystem publishes OTLP message types: `io.opentelemetry.proto:opentelemetry-proto`
for Java, the `opentelemetry-proto` crate for Rust, `go.opentelemetry.io/proto/otlp` for Go. .NET has no
official one. Two unofficial packages do expose the types, `Heimdall.Otlp.Proto` and
`IF.APM.OpenTelemetry.Proto`, and both are `Google.Protobuf` code generation (checked August 2026).

The gap is on the receiving side. An OTLP exporter only ever **writes** the wire format, so a .NET
service that wants to **read** it has had to take on a protobuf runtime and a code generation step, for a
handful of message types that have not changed shape in years.

This package is those types and a decoder for them, hand written against the wire format, with an empty
dependency list.

It is unofficial and unaffiliated with the OpenTelemetry project or the CNCF.

## Reading an export

Pick the method from the request's content type: `ParseProtobuf` for `application/x-protobuf`,
`ParseJson` for `application/json`.

```csharp
using Condux.Otlp;

var result = OtlpLogs.ParseProtobuf(body);

if (!result.IsSuccess)
{
    return Results.BadRequest(result.Error);
}

foreach (var resourceLogs in result.Value.ResourceLogs)
foreach (var scopeLogs in resourceLogs.ScopeLogs)
foreach (var record in scopeLogs.LogRecords)
{
    Console.WriteLine($"{record.SeverityNumber} {record.Body}");
}
```

Both encodings produce the same types, so code downstream of the parse does not know which arrived.

Neither method throws. A payload that does not decode comes back as a failed result carrying a short
description of what was wrong with its shape, never a copy of the payload.

## Answering an export

The protocol requires a server to answer in the same content type it received. A full success is an empty
message in both encodings.

```csharp
var response = new ExportLogsServiceResponse();
return isJson
    ? Results.Text(response.ToJson(), "application/json")
    : Results.Bytes(response.ToProtobuf(), "application/x-protobuf");
```

To report records you rejected, or to pass a warning back on an export you accepted in full:

```csharp
response.PartialSuccess = new ExportLogsPartialSuccess
{
    RejectedLogRecords = 12,
    ErrorMessage = "over the configured ingest limit",
};
```

## Reading values

`AnyValue` is a `oneof`, so `Kind` says which member is set. Reading a member without checking `Kind`
cannot distinguish an integer of zero from a field that was never set.

```csharp
foreach (var attribute in record.Attributes)
{
    var text = attribute.Value?.Kind switch
    {
        AnyValueKind.String => attribute.Value.StringValue,
        AnyValueKind.Int => attribute.Value.IntValue.ToString(),
        _ => null,
    };
}
```

The types carry the data and no formatting. How a value should be shown, and what to do with an array or
a nested map, is the consumer's decision, so the package does not make one.

`SeverityNumber` is an ordered scale with four numbers per named level, so `>= SeverityNumber.Error`
covers every error severity. A sender may use a number this enum does not name, and the decoder keeps it.

`TraceId` and `SpanId` are the raw bytes: sixteen and eight when present, empty when the record belongs to
no trace. Read trace flags through `LogRecord.TraceFlags`, which masks off the bits the protocol reserves,
rather than through `Flags` directly.

## What it does not do

- **Logs only.** Traces and metrics are not implemented.
- **No transport.** Decompressing the body, checking content types and routing `/v1/logs` are the host's
  job. Whether a payload arrives compressed depends on the producer, and the defaults differ: the
  OpenTelemetry Collector's `otlphttp` exporter gzips, while the OpenTelemetry .NET and JS SDK exporters
  send the bytes as they are. A receiver has to honour `Content-Encoding` either way, before the payload
  reaches this package.
- **No semantic conventions.** `exception.type` and friends are attributes like any other. What a
  consumer does with them is not the protocol's business, and so not this package's.
- **No encoder for exports.** It writes the service response, because a receiver has to, and nothing else.

## Limits and strictness

Decoding hostile input is the normal case for a receiver, so the decoder is deliberately strict.

- Nesting is capped at 64 levels in both encodings, counted in nested messages for protobuf and in the
  document's own depth for JSON. A value nests without limit through arrays, so without a cap a small
  payload can exhaust the stack.
- A varint may occupy at most 10 bytes, which is what a 64-bit value needs.
- A field this version does not know is stepped over using its wire type, so a sender on a later version
  of the protocol still decodes. A known field arriving with the wrong wire type is treated the same way.
- A protobuf string must be valid UTF-8. One that is not is rejected rather than repaired, because
  substituting replacement characters hands the caller data the sender never sent.
- The group wire type is rejected. proto3 removed it and no OTLP message uses it.
- In JSON, member names are lowerCamelCase only and enum values must be integers. Both are what the
  encoding requires; neither is a choice this package made.

## Versions

Message types follow [`opentelemetry-proto`](https://github.com/open-telemetry/opentelemetry-proto)
**v1.11.0**. The `.proto` files that version defines are copied unmodified into `proto-reference/` so
every field number here can be checked against its source.

Fields upstream marks as `[Development]` or `[Alpha]` are skipped rather than modelled, because their
shape can still change incompatibly. Today that means `Resource.entity_refs`, and the profiling
string-table indexes on `AnyValue` and `KeyValue`, which the protocol tells a receiver of any other
signal to read as though they were absent.

The package version is `0.x` while the API settles.

## Tests

The fixtures are exports captured from real OpenTelemetry software, never from this package's own
encoder: a decoder checked against its own writer shares its assumptions and cannot disprove them. Three
implementations produced them, the OpenTelemetry .NET exporter, the OpenTelemetry Collector and the
OpenTelemetry JS SDK.

Two of them are a single export relayed by the collector in both encodings. Those decode to the same
message, which is what pins the one rule the encodings disagree about: OTLP writes trace and span ids as
hex where the standard protobuf JSON mapping would use base64. The JS export is the second, independent
producer of the JSON encoding, so the strict reading of member names and enum values above rests on more
than one implementation's habits.

A separate test decodes a hand-built payload that sets every field of every message, in both encodings.
The captured fixtures cannot do that job, because a real exporter emits only what it has, and a field it
leaves at its default would then be read by no test at all.

`tools/capture-fixtures.sh` captures the fixtures again. It needs Docker and Node, and it also checks that
a real collector accepts the response this package writes: it would retry the export if it did not.
Nothing runs it automatically. CI compiles its .NET half, but the collector configuration and the
JavaScript emitter are exercised only when somebody recaptures, so expect to fix them when you do.

## Licence

Apache-2.0. See `NOTICE` for the attribution that `opentelemetry-proto` carries.
