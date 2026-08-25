# Reference definitions

Unmodified copies of the `opentelemetry-proto` files this package implements, at release **v1.11.0**.

They are here so a reader can check any field number in the decoders against the definition it came from,
without leaving the repository or guessing which upstream revision was meant. Nothing compiles them, and
no build step depends on them.

Fetched from `https://github.com/open-telemetry/opentelemetry-proto/tree/v1.11.0/opentelemetry/proto`,
keeping the upstream directory layout:

| File | Message types used here |
|---|---|
| `collector/logs/v1/logs_service.proto` | `ExportLogsServiceRequest`, `ExportLogsServiceResponse`, `ExportLogsPartialSuccess` |
| `logs/v1/logs.proto` | `ResourceLogs`, `ScopeLogs`, `LogRecord`, `SeverityNumber` |
| `common/v1/common.proto` | `AnyValue`, `ArrayValue`, `KeyValueList`, `KeyValue`, `InstrumentationScope` |
| `resource/v1/resource.proto` | `Resource` |

To move to a later release, replace these files, then reconcile every field number in `src/Condux.Otlp`
against them. Upstream adds fields and does not renumber them, so the usual outcome is new fields to
model rather than changed ones.

Licensed under the Apache License, Version 2.0, Copyright The OpenTelemetry Authors. See `NOTICE`.
