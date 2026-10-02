namespace DotNetFrameworkToolkit.Modules.UserAccess;

/// <summary>
/// Represents a data transfer object containing credential material used in cryptographic operations.
/// </summary>
/// <remarks>
/// Persist all three fields together. The current unversioned format uses UTF-8 password
/// bytes, PBKDF2-HMAC-SHA1, and a 32-byte hash. It cannot identify older or future formats.
/// Applications must retain a format identifier outside this DTO before mixing formats.
/// </remarks>
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
