# Changelog

## 0.3.1

- `ParseJson`, for either signal, now also returns a failed result for a `bytesValue` whose string is
  not valid text. 0.3.0 handled the strings read as text; a bytes value is decoded from base64 by a
  different read, which still threw.

## 0.3.0

- **Traces.** `OtlpTraces.ParseProtobuf` and `OtlpTraces.ParseJson` read an OTLP traces export in either
  encoding, into message types for `ExportTraceServiceRequest` and everything it reaches. A span's status
  is `SpanStatus` with `SpanStatusCode`, named for the span because `Status` is already the
  `google.rpc.Status` a failed export is answered with. `ExportTraceServiceResponse` encodes the answer in
  both encodings.
- `ParseJson`, for either signal, returns a failed result instead of throwing when a JSON string is not
  valid text: invalid UTF-8, or an escape for an unpaired surrogate. The parser accepts such a document
  and the failure surfaced only when a string was read, so 0.2.0 broke its own promise not to throw.
- **Breaking:** `OtlpReadResult` is now `OtlpReadResult<TExport>`, since a parse returns a different
  export per signal. Code that holds a result in a `var` is unaffected; code that names the type adds
  `<ExportLogsServiceRequest>`.
- The fixture capture tool records traces from the same three producers as logs, and maps
  `host.docker.internal` explicitly, which Docker Engine on Linux does not define by itself.
- The package names its maker, Triple Down AB, with a postal address and the address for vulnerability
  reports, security@condux.ai. They are in the package's authors and copyright fields and on the first
  line of `LICENSE`, and the package now carries `LICENSE`, `NOTICE` and `SECURITY.md`, which says how to
  report a vulnerability. No code change.

## 0.2.0

- `Status` encodes to protobuf or JSON, so a receiver can refuse an export the way the protocol
  requires: the body of every `4xx` and `5xx` must be a `google.rpc.Status`, in the content type the
  request arrived in. Until now the package wrote only the success response, so a receiver built on it
  had nothing conformant to answer a failure with. Found by using it.
- `StatusCode` carries the canonical `google.rpc.Code` values. `Status.Code` stays an `int`, because the
  field is an `int32` and a code outside the named set is legal.
- The `details` field is deliberately not modelled. It is a repeated `google.protobuf.Any`, which cannot
  be written without an Any implementation, and a repeated field with no entries is identical on the
  wire to one left out.

## 0.1.1

- The package carries an icon. No code change: 0.1.0 and 0.1.1 are the same library.

## 0.1.0

- `OtlpLogs.ParseProtobuf` and `OtlpLogs.ParseJson` read an OTLP logs export in either encoding into the
  same message types. Neither throws.
- Message types for `ExportLogsServiceRequest` and everything it reaches, following `opentelemetry-proto`
  v1.11.0.
- `ExportLogsServiceResponse` encodes to protobuf or JSON, so a receiver can answer in the content type
  it was sent.
- No package dependencies, enforced in CI.
