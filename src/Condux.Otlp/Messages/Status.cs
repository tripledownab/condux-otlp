namespace Condux.Otlp;

/// <summary>
/// The body a server answers a FAILED export with. <c>google.rpc.Status</c>.
/// </summary>
/// <remarks>
/// OTLP/HTTP requires this: the response body for every 4xx and 5xx must be a Status describing the
/// problem, encoded the same way the request was. Answering an empty body, or a shape of the receiver's
/// own invention, leaves the sender with only the status code to act on.
/// <para>
/// The protocol also decides what a sender does next, and the code here does not change that: a 400 is
/// never retried, a 429 or 503 is retried after a backoff. So this message is what makes a failure
/// legible to whoever has to fix it, not a way to ask for a retry.
/// </para>
/// <para>
/// The <c>details</c> field of the upstream message is not modelled. It is a repeated
/// <c>google.protobuf.Any</c>, which cannot be written without an Any implementation and the descriptors
/// to go with it, and a repeated field with no entries is identical on the wire to one left out. Nothing
/// a receiver needs to say about a rejected export requires it.
/// </para>
/// </remarks>
public sealed class Status
{
    /// <summary>
    /// Field 1. One of <see cref="StatusCode"/>, as a plain integer. It is an <c>int32</c> on the wire
    /// rather than an enum, so a value outside the named set is legal and must not be rejected.
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Field 2. A developer-facing message in English explaining what went wrong. It reaches a log
    /// somebody reads while an export is failing, so say what to change.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>Encodes the status as binary protobuf, for a request that arrived as protobuf.</summary>
    public byte[] ToProtobuf() => StatusEncoder.ToProtobuf(this);

    /// <summary>Encodes the status as OTLP JSON, for a request that arrived as JSON.</summary>
    public string ToJson() => StatusEncoder.ToJson(this);
}
