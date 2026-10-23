using Xunit;

namespace Condux.Otlp.Tests;

/// <summary>
/// JSON whose strings do not hold valid text. The parser accepts such a document and the read of the
/// string throws later, so these are the inputs that broke the promise that parsing never throws. Each
/// case runs through both signals, since both enter through one place.
/// </summary>
public class InvalidTextTests
{
    // A string member holding an escape for an unpaired surrogate, which no UTF-16 text can contain.
    private const string LoneSurrogateLogs = """{"resourceLogs":[{"schemaUrl":"\uD800"}]}""";
    private const string LoneSurrogateTraces = """{"resourceSpans":[{"scopeSpans":[{"spans":[{"name":"\uDC00"}]}]}]}""";

    [Fact]
    public void RefusesAnEscapedLoneSurrogateInsteadOfThrowing()
    {
        AssertRefusedAsText(OtlpLogs.ParseJson(LoneSurrogateLogs));
        AssertRefusedAsText(OtlpTraces.ParseJson(LoneSurrogateTraces));
    }

    /// <summary>
    /// A bytes value is a base64 string, and decoding it unescapes the string first, so it is text that
    /// has to be valid like any other. Here in a log body and in a span attribute.
    /// </summary>
    [Fact]
    public void RefusesAnEscapedLoneSurrogateInABytesValueInsteadOfThrowing()
    {
        AssertRefusedAsText(OtlpLogs.ParseJson(
            """{"resourceLogs":[{"scopeLogs":[{"logRecords":[{"body":{"bytesValue":"\uD800"}}]}]}]}"""));
        AssertRefusedAsText(OtlpTraces.ParseJson(
            """{"resourceSpans":[{"scopeSpans":[{"spans":[{"attributes":[{"key":"k","value":{"bytesValue":"\uDC00"}}]}]}]}]}"""));
    }

    /// <summary>A byte that is not valid UTF-8, inside a string the decoder reads.</summary>
    [Fact]
    public void RefusesInvalidUtf8InAStringInsteadOfThrowing()
    {
        AssertRefusedAsText(OtlpLogs.ParseJson(WithByte("""{"resourceLogs":[{"schemaUrl":"a"}]}""")));
        AssertRefusedAsText(OtlpTraces.ParseJson(WithByte("""{"resourceSpans":[{"scopeSpans":[{"spans":[{"name":"a"}]}]}]}""")));
    }

    /// <summary>Text handed in as a string with a raw unpaired surrogate, which cannot become UTF-8.</summary>
    [Fact]
    public void RefusesARawLoneSurrogateInStringInputInsteadOfThrowing()
    {
        var payload = "{\"resourceSpans\":[{\"schemaUrl\":\"" + '\uD800' + "\"}]}";

        AssertRefusedAsText(OtlpLogs.ParseJson(payload));
        AssertRefusedAsText(OtlpTraces.ParseJson(payload));
    }

    /// <summary>
    /// A member name the decoder never asks for still has to be compared during a lookup, so an invalid
    /// escape in it is found and reported rather than thrown.
    /// </summary>
    [Fact]
    public void RefusesAnInvalidEscapeInAnUnknownMemberName()
    {
        AssertRefusedAsText(OtlpTraces.ParseJson("""{"resourceSpans":[{"scopeSpans":[{"spans":[{"\uD800":1,"name":"a"}]}]}]}"""));
    }

    /// <summary>
    /// Values of the wrong kind are refused by the decoder's own checks, and reported as a payload that is
    /// not an export rather than as bad text. These pin the checks the bad-text handling must not hide:
    /// without them, a missing check would surface as a misleading message instead of a failing test.
    /// </summary>
    [Theory]
    [InlineData("""{"resourceSpans":{}}""")]
    [InlineData("""{"resourceSpans":[{"scopeSpans":[{"spans":[{"attributes":"x"}]}]}]}""")]
    [InlineData("""{"resourceSpans":[{"scopeSpans":[{"spans":[{"name":7}]}]}]}""")]
    [InlineData("""{"resourceSpans":[{"scopeSpans":[{"spans":[{"traceId":7}]}]}]}""")]
    public void RefusesAValueOfTheWrongKindAsNotAnExport(string payload)
    {
        var result = OtlpTraces.ParseJson(payload);

        Assert.False(result.IsSuccess);
        Assert.Contains("not a JSON-encoded OTLP traces export", result.Error);
    }

    [Theory]
    [InlineData("""{"resourceLogs":{}}""")]
    [InlineData("""{"resourceLogs":[{"scopeLogs":[{"logRecords":[{"attributes":"x"}]}]}]}""")]
    [InlineData("""{"resourceLogs":[{"scopeLogs":[{"logRecords":[{"severityText":7}]}]}]}""")]
    public void RefusesALogsValueOfTheWrongKindAsNotAnExport(string payload)
    {
        var result = OtlpLogs.ParseJson(payload);

        Assert.False(result.IsSuccess);
        Assert.Contains("not a JSON-encoded OTLP logs export", result.Error);
    }

    /// <summary>A null payload is the caller's bug rather than hostile input, so it still throws.</summary>
    [Fact]
    public void StillThrowsForANullPayload()
    {
        Assert.Throws<ArgumentNullException>(() => OtlpLogs.ParseJson((string)null!));
        Assert.Throws<ArgumentNullException>(() => OtlpTraces.ParseJson((string)null!));
    }

    private static void AssertRefusedAsText<TExport>(OtlpReadResult<TExport> result)
        where TExport : class
    {
        Assert.False(result.IsSuccess);
        Assert.Contains("UTF-8", result.Error);
    }

    /// <summary>The UTF-8 bytes of the JSON, with the one-character string value "a" replaced by 0xFF.</summary>
    private static byte[] WithByte(string json)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var at = Array.IndexOf(bytes, (byte)'"', Array.LastIndexOf(bytes, (byte)':') + 1) + 1;
        bytes[at] = 0xFF;
        return bytes;
    }
}
