// Copyright © 2025 Roby Van Damme.

namespace DotBump.Common;

/// <summary>
/// Represents a DotBump specific exception.
/// </summary>
public class DotBumpException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotBumpException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public DotBumpException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotBumpException"/> class.
    /// </summary>
    public DotBumpException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotBumpException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DotBumpException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
