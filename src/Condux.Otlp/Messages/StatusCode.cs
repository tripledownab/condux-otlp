namespace Condux.Otlp;

/// <summary>
/// The canonical error codes a <see cref="Status"/> carries. <c>google.rpc.Code</c>.
/// </summary>
/// <remarks>
/// The set is closed and the numbers are part of the contract, so a receiver picks the code that
/// describes the problem rather than inventing one. <see cref="Status.Code"/> is a plain integer on the
/// wire, which is why this is an ordinary enum rather than the field's own type: a sender is free to use
/// a number this does not name, and a reader must not reject it.
/// </remarks>
public enum StatusCode
{
    /// <summary>0. Not an error. A failure response should never carry this.</summary>
    Ok = 0,

    /// <summary>1. The operation was cancelled, typically by the caller.</summary>
    Cancelled = 1,

    /// <summary>2. An error whose cause does not fit any other code.</summary>
    Unknown = 2,

    /// <summary>3. The request was malformed. The client should not retry it unchanged.</summary>
    InvalidArgument = 3,

    /// <summary>4. The deadline passed before the operation finished.</summary>
    DeadlineExceeded = 4,

    /// <summary>5. The thing asked for does not exist.</summary>
    NotFound = 5,

    /// <summary>6. The thing being created already exists.</summary>
    AlreadyExists = 6,

    /// <summary>7. The caller is authenticated but not allowed to do this.</summary>
    PermissionDenied = 7,

    /// <summary>8. A resource is exhausted: a quota, a rate limit, or a size ceiling.</summary>
    ResourceExhausted = 8,

    /// <summary>9. The system is not in a state where the operation can run.</summary>
    FailedPrecondition = 9,

    /// <summary>10. The operation was aborted, typically by a concurrency conflict.</summary>
    Aborted = 10,

    /// <summary>11. The operation was attempted past the valid range.</summary>
    OutOfRange = 11,

    /// <summary>12. The operation is not implemented or not supported here.</summary>
    Unimplemented = 12,

    /// <summary>13. An internal fault. Something the receiver expected to hold did not.</summary>
    Internal = 13,

    /// <summary>14. The service is unavailable. This one is retryable, after a backoff.</summary>
    Unavailable = 14,

    /// <summary>15. Data was lost or corrupted unrecoverably.</summary>
    DataLoss = 15,

    /// <summary>16. The caller could not be authenticated.</summary>
    Unauthenticated = 16,
}
