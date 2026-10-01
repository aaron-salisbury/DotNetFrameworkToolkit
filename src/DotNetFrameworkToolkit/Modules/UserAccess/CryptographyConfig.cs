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
    /// Calibrate this value for deployment hardware. The compatibility default is not a security recommendation.
    /// </remarks>
    public int NewUserWorkFactor { get; set; } = 10000;

    /// <summary>
    /// Upper bound on iterations accepted from stored credential data.
    /// </summary>
    public int MaxVerificationWorkFactor { get; set; } = 1000000;
}
