using System.Buffers;
using System.Text;
using System.Text.Json;
using Condux.Otlp.Protobuf;

namespace Condux.Otlp;

/// <summary>
/// Encodes the status a server answers a failed export with. See <see cref="Status"/>.
/// </summary>
internal static class StatusEncoder
{
    // Field numbers from google/rpc/status.proto.
    private const int CodeField = 1;
    private const int MessageField = 2;

    internal static byte[] ToProtobuf(Status status)
    {
        var writer = new ProtobufWriter();
        // Written through the 64-bit path deliberately. A varint for an int32 and for an int64 are the
        // same bytes across the whole int32 range, including negatives, which proto3 sign-extends to 64
        // bits. A separate 32-bit writer would be a second implementation of one encoding.
        writer.WriteInt64(CodeField, status.Code);
        writer.WriteString(MessageField, status.Message);
        return writer.ToArray();
    }

    internal static string ToJson(Status status)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();

            // A 32-bit integer travels as a JSON number, unlike the 64-bit counts elsewhere in the
            // protocol, which travel as decimal strings because a JSON number cannot hold their range.
            if (status.Code != 0)
            {
                writer.WriteNumber("code", status.Code);
            }

            if (status.Message.Length != 0)
            {
                writer.WriteString("message", status.Message);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
