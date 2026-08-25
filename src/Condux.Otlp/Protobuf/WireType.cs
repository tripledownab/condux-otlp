namespace Condux.Otlp.Protobuf;

/// <summary>The three low bits of a protobuf tag, which say how to read the value that follows.</summary>
internal enum WireType
{
    Varint = 0,
    Fixed64 = 1,
    LengthDelimited = 2,
    StartGroup = 3,
    EndGroup = 4,
    Fixed32 = 5,
}
