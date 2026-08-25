using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Condux.Otlp.Protobuf;

namespace Condux.Otlp;

/// <summary>
/// Encodes the response a server answers an export with. See <see cref="ExportLogsServiceResponse"/>.
/// </summary>
/// <remarks>
/// A full success is an empty message in both encodings, which is why the common case costs no bytes in
/// protobuf and two in JSON.
/// </remarks>
internal static class ExportLogsResponseEncoder
{
    // Field numbers from collector/logs/v1/logs_service.proto. The inner two are the fields of
    // ExportLogsPartialSuccess, which is why a 1 appears twice.
    private const int PartialSuccessField = 1;
    private const int RejectedLogRecordsField = 1;
    private const int ErrorMessageField = 2;

    internal static byte[] ToProtobuf(ExportLogsServiceResponse response)
    {
        if (response.PartialSuccess is null)
        {
            return [];
        }

        var partialSuccess = new ProtobufWriter();
        partialSuccess.WriteInt64(RejectedLogRecordsField, response.PartialSuccess.RejectedLogRecords);
        partialSuccess.WriteString(ErrorMessageField, response.PartialSuccess.ErrorMessage);

        var writer = new ProtobufWriter();
        writer.WriteMessage(PartialSuccessField, partialSuccess);
        return writer.ToArray();
    }

    internal static string ToJson(ExportLogsServiceResponse response)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            if (response.PartialSuccess is not null)
            {
                writer.WriteStartObject("partialSuccess");
                if (response.PartialSuccess.RejectedLogRecords != 0)
                {
                    // A 64-bit integer travels as a decimal string, since a JSON number cannot hold the
                    // whole range exactly.
                    writer.WriteString(
                        "rejectedLogRecords",
                        response.PartialSuccess.RejectedLogRecords.ToString(CultureInfo.InvariantCulture));
                }

                if (response.PartialSuccess.ErrorMessage.Length != 0)
                {
                    writer.WriteString("errorMessage", response.PartialSuccess.ErrorMessage);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
