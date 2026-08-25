using Condux.Otlp.Protobuf;
using Xunit;

namespace Condux.Otlp.Tests;

/// <summary>
/// Covers the response a server answers an export with. The capture run in
/// <c>tools/capture-fixtures.sh</c> is the other half of this: a real collector accepts these bytes in
/// both encodings, and would retry the export if it did not.
/// </summary>
public class ResponseTests
{
    /// <summary>
    /// A full success is an empty message, so it costs no bytes at all in protobuf. Writing an empty
    /// partial-success submessage instead would say the same thing in more bytes, but a receiver reading
    /// the count would then find a field where the protocol says there is none.
    /// </summary>
    [Fact]
    public void EncodesAFullSuccessAsAnEmptyMessage()
    {
        var response = new ExportLogsServiceResponse();

        Assert.Empty(response.ToProtobuf());
        Assert.Equal("{}", response.ToJson());
    }

    [Fact]
    public void EncodesARejectionCountAndMessageAsProtobuf()
    {
        var response = new ExportLogsServiceResponse
        {
            PartialSuccess = new ExportLogsPartialSuccess { RejectedLogRecords = 3, ErrorMessage = "over quota" },
        };

        // partial_success is field 1 of the response, and inside it the count is field 1 and the message
        // field 2. Reading the bytes back checks the tags, not only the values. The reader used here is
        // the library's own, which the captured fixtures already hold to real exporters' output.
        var reader = new ProtobufReader(response.ToProtobuf());
        Assert.True(reader.TryReadTag(out var field, out var wireType));
        Assert.Equal((1, WireType.LengthDelimited), (field, wireType));
        Assert.True(reader.TryReadLengthDelimited(out var body));

        var partial = new ProtobufReader(body);
        Assert.True(partial.TryReadTag(out field, out wireType));
        Assert.Equal((1, WireType.Varint), (field, wireType));
        Assert.True(partial.TryReadVarint(out var rejected));
        Assert.Equal(3UL, rejected);
        Assert.True(partial.TryReadTag(out field, out wireType));
        Assert.Equal((2, WireType.LengthDelimited), (field, wireType));
        Assert.True(partial.TryReadString(out var message));
        Assert.Equal("over quota", message);
        Assert.True(partial.IsAtEnd);
    }

    /// <summary>
    /// A 64-bit count travels as a decimal string in JSON, the same rule the decoder reads timestamps by.
    /// </summary>
    [Fact]
    public void EncodesARejectionCountAsAStringInJson()
    {
        var response = new ExportLogsServiceResponse
        {
            PartialSuccess = new ExportLogsPartialSuccess { RejectedLogRecords = 3, ErrorMessage = "over quota" },
        };

        Assert.Equal("""{"partialSuccess":{"rejectedLogRecords":"3","errorMessage":"over quota"}}""", response.ToJson());
    }

    /// <summary>
    /// The protocol uses a zero count with a message to carry a warning about an export it accepted in
    /// full, so the message has to survive a count of zero.
    /// </summary>
    [Fact]
    public void KeepsAWarningOnAnExportItAcceptedInFull()
    {
        var response = new ExportLogsServiceResponse
        {
            PartialSuccess = new ExportLogsPartialSuccess { ErrorMessage = "an attribute was truncated" },
        };

        Assert.Equal("""{"partialSuccess":{"errorMessage":"an attribute was truncated"}}""", response.ToJson());
        Assert.NotEmpty(response.ToProtobuf());
    }

    /// <summary>
    /// A partial success carrying neither a count nor a message is equivalent to sending none, and the
    /// caller set it deliberately, so it is written rather than dropped.
    /// </summary>
    [Fact]
    public void WritesAnEmptyPartialSuccessTheCallerAskedFor()
    {
        var response = new ExportLogsServiceResponse { PartialSuccess = new ExportLogsPartialSuccess() };

        Assert.Equal("""{"partialSuccess":{}}""", response.ToJson());
        Assert.Equal([0x0A, 0x00], response.ToProtobuf());
    }
}
