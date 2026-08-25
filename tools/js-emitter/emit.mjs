// Emits one OTLP logs export through the OpenTelemetry JS SDK.
//
// Its HTTP exporter writes the JSON encoding directly, without a collector in between, so this is a
// second and independent implementation of that encoding. Decoding what it produces is what shows the
// decoder's strict reading of member names and enum values is the encoding's rule rather than one
// producer's habit.
import { context, trace } from '@opentelemetry/api';
import { SeverityNumber } from '@opentelemetry/api-logs';
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-http';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { BatchLogRecordProcessor, LoggerProvider } from '@opentelemetry/sdk-logs';
import { BasicTracerProvider } from '@opentelemetry/sdk-trace-base';

const url = process.argv[2] ?? 'http://127.0.0.1:5399/js/v1/logs';

const loggerProvider = new LoggerProvider({
  resource: resourceFromAttributes({ 'service.name': 'checkout-api', 'service.version': '1.4.2' }),
  // Batching, so both records leave in one export the way a real service sends them. Shutdown flushes,
  // so the export is complete when this script exits.
  processors: [new BatchLogRecordProcessor({ exporter: new OTLPLogExporter({ url }) })],
});

const logger = loggerProvider.getLogger('Checkout.Payments');

logger.emit({
  severityNumber: SeverityNumber.INFO,
  severityText: 'Information',
  body: 'Charging 42.5 EUR for order ord_7Q2',
  attributes: { Amount: 42.5, Currency: 'EUR', OrderId: 'ord_7Q2' },
});

// The second record belongs to a span, so the export carries a trace and a span id. Those are the two
// fields the encoding writes as hex rather than the base64 a generic protobuf-to-JSON mapping uses.
//
// The context goes to emit directly rather than through context.with. Nothing here registers a context
// manager, and without one the ambient context never leaves its root, so the ids would silently be
// absent and the fixture would cover less than it appears to.
const span = new BasicTracerProvider().getTracer('checkout').startSpan('charge');
logger.emit({
  severityNumber: SeverityNumber.ERROR,
  severityText: 'Error',
  body: 'Payment provider rejected the charge for order ord_7Q2',
  context: trace.setSpan(context.active(), span),
  attributes: {
    'exception.type': 'InvalidOperationException',
    'exception.message': 'card_declined: insufficient funds',
    OrderId: 'ord_7Q2',
  },
});
span.end();

await loggerProvider.shutdown();
