using DotNetFrameworkToolkit.Core.Extensions;
using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;
using System;

namespace DotNetFrameworkToolkit.Modules.UserAccess;

/// <summary>
/// Defines methods for user authentication and credential management,
/// including secure credential creation and password verification.
/// </summary>
/// <remarks>
/// This implementation uses Patterns & Practices Enterprise Library and
/// is inspired by this <see href="https://www.mking.net/blog/password-security-best-practices-with-examples-in-csharp">article</see> by Matthew King.
/// </remarks>
internal class UserAuthenticator : HashAlgorithmProvider, IUserAuthenticator
{
    private readonly CryptographyConfig _cryptographyConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAuthenticator"/> class with cryptographic configuration settings.
    /// </summary>
    /// <param name="cryptographyConfig">
    /// The configuration settings for cryptographic operations, such as salt length, work factor, and hash algorithm.
    /// If <c>null</c>, default settings will be used.
    /// </param>
    public UserAuthenticator(CryptographyConfig cryptographyConfig) : base(cryptographyConfig.HashAlgorithm.GetType(), saltEnabled: true)
    {
        _cryptographyConfig = cryptographyConfig;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Does not enforce any password policy.
    /// </remarks>
    public CryptographyCredential CreateUserCredentials(string password)
    {
        byte[] loginSalt = GenerateSalt();
        byte[] pBytes = password.ToBytes();

        try
        {
            return new CryptographyCredential()
            {
                LoginSalt = loginSalt,
                LoginHash = GenerateHash(pBytes, loginSalt, _cryptographyConfig.NewUserWorkFactor),
                LoginWorkFactor = _cryptographyConfig.NewUserWorkFactor
            };
        }
        finally
        {
            Array.Clear(pBytes, 0, pBytes.Length);
        }
    }

    /// <inheritdoc/>
    public bool VerifyCredentials(CryptographyCredential credential, string password)
    {
        byte[] pBytes = password.ToBytes();

        try
        {
            byte[] checkHash = GenerateHash(pBytes, credential.LoginSalt, credential.LoginWorkFactor);

            return CryptographyUtility.CompareBytes(checkHash, credential.LoginHash);
        }
        finally
        {
            Array.Clear(pBytes, 0, pBytes.Length);
        }
    }

    private byte[] GenerateSalt()
    {
        return CryptographyUtility.GetRandomBytes(_cryptographyConfig.SaltLength);
    }

    private byte[] GenerateHash(byte[] password, byte[] salt, int workFactor)
    {
        workFactor = workFactor > 0 ? workFactor : 1;
        byte[] runningHash = password;

        for (int i = 0; i < workFactor; i++)
        {
            runningHash = CreateHashWithSalt(runningHash, salt);
        }

        return runningHash;
    }
}
