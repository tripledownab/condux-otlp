# Changelog

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
