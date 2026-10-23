// Emits one OTLP traces export through the OpenTelemetry JS SDK.
//
// Like emit.mjs, this is a second and independent implementation of the JSON encoding. The batch mirrors
// the .NET emitter's: a server span with a client span under it, the child carrying an event, an
// exception, a link to a span in another trace and an error status, the parent an ok status.
import { context, SpanKind, SpanStatusCode, trace } from '@opentelemetry/api';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { BasicTracerProvider, BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';

const url = process.argv[2] ?? 'http://127.0.0.1:5399/js/v1/traces';

const provider = new BasicTracerProvider({
  resource: resourceFromAttributes({ 'service.name': 'checkout-api', 'service.version': '1.4.2' }),
  // Batching, so both spans leave in one export. Shutdown flushes, so the export is complete when this
  // script exits.
  spanProcessors: [new BatchSpanProcessor(new OTLPTraceExporter({ url }))],
});
const tracer = provider.getTracer('checkout');

// A stack trace carries the absolute paths of the machine it ran on, and a fixture is published. The
// paths are rewritten here, at the source, so a capture is identical wherever it runs.
function failure() {
  const error = new Error('card_declined: insufficient funds');
  error.name = 'InvalidOperationError';
  error.stack = error.stack.replaceAll(process.cwd(), '/capture');
  return error;
}

// Contexts are passed explicitly: nothing registers a context manager, so the ambient context would
// never leave its root and the child would silently lose its parent.
const request = tracer.startSpan('POST /checkout', {
  kind: SpanKind.SERVER,
  attributes: { 'http.request.method': 'POST', 'http.response.status_code': 502 },
});

const retried = { traceId: '5b8efff798038103d269b633813fc60c', spanId: 'eee19b7ec3c1b174', traceFlags: 1 };
const charge = tracer.startSpan(
  'charge',
  {
    kind: SpanKind.CLIENT,
    attributes: { 'payment.provider': 'acme-pay', 'payment.amount': 42.5 },
    links: [{ context: retried, attributes: { 'link.reason': 'retry of an earlier attempt' } }],
  },
  trace.setSpan(context.active(), request),
);
charge.addEvent('provider.request.sent');
charge.recordException(failure());
charge.setStatus({ code: SpanStatusCode.ERROR, message: 'card_declined' });
charge.end();

request.setStatus({ code: SpanStatusCode.OK });
request.end();

await provider.shutdown();
