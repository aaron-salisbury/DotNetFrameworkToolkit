using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;
using System.Security.Cryptography;

namespace DotNetFrameworkToolkit.Modules.UserAccess;

/// <summary>
/// Represents configuration settings for cryptographic operations used in user authentication.
/// </summary>
public class CryptographyConfig
{
    /// <summary>
    /// The length, in bytes, of the cryptographic salt to be generated for password hashing.
    /// </summary>
    public int SaltLength { get; set; } = HashAlgorithmProvider.SaltLength;

    /// <summary>
    /// The work factor to use for key derivation functions such as PBKDF2 when creating new hashes.
    /// Higher values increase computational cost and security.
    /// </summary>
    /// <remarks>
    /// OWASP recommends at least 10,000 iterations, but the ideal value depends on your hardware and performance requirements.
    /// </remarks>
    public int NewUserWorkFactor { get; set; } = 10000;

    /// <summary>
    /// The hash algorithm to use for password hashing (e.g., "SHA256Managed").
    /// </summary>
    /// <remarks>
    /// SHA-256 is a widely supported choice for PBKDF2.
    /// </remarks>
    public HashAlgorithm HashAlgorithm { get; set; } = new SHA256Managed();
}
