using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;
using Microsoft.Practices.Unity.Utility;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace DotNetFrameworkToolkit.Modules.UserAccess;

/// <summary>
/// Defines methods for user authentication and credential management, 
/// including secure credential creation and password verification.
/// </summary>
/// <remarks>
/// Creates versioned UTF-8 PBKDF2-HMAC-SHA1 credentials using
/// Patterns & Practices Enterprise Library (.Net Framework 2.0).
/// Inspired by this <see href="https://www.mking.net/blog/password-security-best-practices-with-examples-in-csharp">article</see> by Matthew King.
/// </remarks>
public sealed class UserAuthenticator : IUserAuthenticator
{
    private readonly int _saltLength, _workFactor, _maxWorkFactor;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAuthenticator"/> class with cryptographic configuration settings.
    /// </summary>
    /// <param name="config">
    /// The configuration settings for cryptographic operations, such as salt length and work factor.
    /// If <c>null</c>, default settings will be used.
    /// </param>
    /// <remarks>
    /// Snapshots configuration. Null selects defaults. Calibrate the work factor for the application.
    /// </remarks>
    public UserAuthenticator(CryptographyConfig config = null)
    {
        if (config is not null)
        {
            if (config.SaltLength < 8 || config.SaltLength > 1024)
            {
                throw new ArgumentOutOfRangeException(nameof(config.SaltLength));
            }

            if (config.MaxVerificationWorkFactor < 1 || config.NewUserWorkFactor < 1 || config.NewUserWorkFactor > config.MaxVerificationWorkFactor)
            {
                throw new ArgumentOutOfRangeException(nameof(config.NewUserWorkFactor));
            }
        }
        else
        {
            config = new CryptographyConfig();
        }

        _saltLength = config.SaltLength; 
        _workFactor = config.NewUserWorkFactor; 
        _maxWorkFactor = config.MaxVerificationWorkFactor;
    }

    /// <inheritdoc/>
    public CryptographyCredential CreateUserCredentials(string password)
    {
        Guard.ArgumentNotNull(password, nameof(password));

        byte[] salt = CryptographyUtility.GetRandomBytes(_saltLength);
        byte[] bytes = Encoding.UTF8.GetBytes(password);

        try
        {
            Rfc2898DeriveBytes derive = new(bytes, salt, _workFactor);

            return new CryptographyCredential 
            {
                LoginSalt = salt, 
                LoginHash = derive.GetBytes(32), 
                LoginWorkFactor = _workFactor 
            };
        }
        finally
        {
            Array.Clear(bytes, 0, bytes.Length);
        }
    }

    /// <inheritdoc/>
    public bool VerifyCredentials(CryptographyCredential credential, string password)
    {
        Guard.ArgumentNotNull(credential, nameof(credential));
        Guard.ArgumentNotNull(password, nameof(password));

        int iterations = credential.LoginWorkFactor;
        byte[] sourceSalt = credential.LoginSalt;
        byte[] sourceHash = credential.LoginHash;

        if (sourceSalt == null || sourceHash == null || sourceSalt.Length > 1024 || sourceHash.Length > 1024 || iterations > _maxWorkFactor)
        {
            return false;
        }

        byte[] salt = (byte[])sourceSalt.Clone();
        byte[] expected = (byte[])sourceHash.Clone();
        byte[] bytes;

        if (salt.Length < 8 || expected.Length != 32 || iterations < 1)
        {
            return false;
        }

        bytes = Encoding.UTF8.GetBytes(password);

        byte[] actual = null;

        try
        {
            Rfc2898DeriveBytes derive = new(bytes, salt, iterations);
            actual = derive.GetBytes(32);

            if (actual.Length != expected.Length)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < actual.Length; i++)
            {
                difference |= actual[i] ^ expected[i];
            }

            return difference == 0;
        }
        finally
        {
            Array.Clear(bytes, 0, bytes.Length);

            if (actual != null)
            {
                Array.Clear(actual, 0, actual.Length);
            }
        }
    }
}
