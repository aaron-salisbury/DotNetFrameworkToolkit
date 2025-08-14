using System;

namespace DotNetFrameworkToolkit.Modules.UserAccess;

/// <summary>
/// Represents a data transfer object containing credential material used in cryptographic operations.
/// </summary>
public class CryptographyCredential
{
    /// <summary>
    /// Cryptographic salt used for hashing the user's login credentials.
    /// </summary>
    public byte[] LoginSalt { get; set; }

    /// <summary>
    /// Hashed value of the user's login credentials.
    /// </summary>
    public byte[] LoginHash { get; set; }

    /// <summary>
    /// Work factor (e.g., cost parameter) used in the password hashing algorithm.
    /// </summary>
    public int LoginWorkFactor { get; set; }
}
