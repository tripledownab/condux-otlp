using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Condux.Otlp.Protobuf;

namespace Condux.Otlp;

/// <summary>
/// Encodes the response a server answers an export with, for every signal. See
/// <see cref="ExportLogsServiceResponse"/> and <see cref="ExportTraceServiceResponse"/>.
/// </summary>
/// <remarks>
/// The signals' responses have one shape: an optional partial success holding a rejected count and a
/// message, at the same field numbers. Only the JSON name of the count differs, so the caller passes it.
/// A full success is an empty message in both encodings, which is why the common case costs no bytes in
/// protobuf and two in JSON.
/// </remarks>
internal static class ExportResponseEncoder
{
    // Field numbers from the collector service .proto files, identical for every signal. The inner two
    // are the fields of the partial success message, which is why a 1 appears twice.
    private const int PartialSuccessField = 1;
    private const int RejectedCountField = 1;
    private const int ErrorMessageField = 2;

    // The JSON member name of the rejected count, which is the one place the signals differ.
    internal const string RejectedLogRecordsName = "rejectedLogRecords";
    internal const string RejectedSpansName = "rejectedSpans";

    /// <summary>Encodes the response. A null <paramref name="partialSuccess"/> means it is absent.</summary>
    internal static byte[] ToProtobuf((long Rejected, string ErrorMessage)? partialSuccess)
    {
        if (partialSuccess is not { } partial)
        {
            return [];
        }

        var inner = new ProtobufWriter();
        inner.WriteInt64(RejectedCountField, partial.Rejected);
        inner.WriteString(ErrorMessageField, partial.ErrorMessage);

        var writer = new ProtobufWriter();
        writer.WriteMessage(PartialSuccessField, inner);
        return writer.ToArray();
    }

    /// <summary>Encodes the response. A null <paramref name="partialSuccess"/> means it is absent.</summary>
    internal static string ToJson((long Rejected, string ErrorMessage)? partialSuccess, string rejectedName)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            if (partialSuccess is { } partial)
            {
                writer.WriteStartObject("partialSuccess");
                if (partial.Rejected != 0)
                {
                    // A 64-bit integer travels as a decimal string, since a JSON number cannot hold the
                    // whole range exactly.
                    writer.WriteString(rejectedName, partial.Rejected.ToString(CultureInfo.InvariantCulture));
                }

                if (partial.ErrorMessage.Length != 0)
                {
                    writer.WriteString("errorMessage", partial.ErrorMessage);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
