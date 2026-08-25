namespace Condux.Otlp;

/// <summary>
/// Fixed decoding limits. They are constants rather than options because they exist to bound hostile
/// input, and an option to raise them is an option to remove the bound.
/// </summary>
internal static class Limits
{
    /// <summary>
    /// How deeply nested messages may go before a payload is rejected. The logs tree is six levels deep,
    /// but <c>AnyValue</c> nests without limit through <c>ArrayValue</c> and <c>KeyValueList</c>, so a
    /// crafted payload could otherwise recurse until the stack is exhausted.
    /// </summary>
    internal const int MaxDepth = 64;

    /// <summary>
    /// How many bytes a varint may occupy. Sixty-four bits at seven bits per byte needs ten. A longer
    /// run of continuation bytes is malformed, and reading it would loop over the whole buffer.
    /// </summary>
    internal const int MaxVarintBytes = 10;
}
