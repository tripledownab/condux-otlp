# Changelog

## 0.1.0

- `OtlpLogs.ParseProtobuf` and `OtlpLogs.ParseJson` read an OTLP logs export in either encoding into the
  same message types. Neither throws.
- Message types for `ExportLogsServiceRequest` and everything it reaches, following `opentelemetry-proto`
  v1.11.0.
- `ExportLogsServiceResponse` encodes to protobuf or JSON, so a receiver can answer in the content type
  it was sent.
- No package dependencies, enforced in CI.
