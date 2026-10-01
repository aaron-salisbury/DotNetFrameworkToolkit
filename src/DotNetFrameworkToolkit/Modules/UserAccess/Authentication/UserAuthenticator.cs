using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;
using System;
using System.Security.Cryptography;
using System.Text;
namespace DotNetFrameworkToolkit.Modules.UserAccess;

/// <summary>Creates versioned UTF-8 PBKDF2-HMAC-SHA1 credentials using the .NET 2.0 implementation.
/// Version zero verifies the historical ASCII/repeated-salted-hash format; rehash after successful login.</summary>
public sealed class UserAuthenticator : IUserAuthenticator
{
    private const string Algorithm = "PBKDF2-HMAC-SHA1";
    private readonly int saltLength, workFactor, maxWorkFactor;
    private readonly Type legacyAlgorithm;
    /// <summary>Snapshots configuration. Null selects defaults. Calibrate the work factor for the application.</summary>
    public UserAuthenticator(CryptographyConfig cryptographyConfig = null)
    {
        CryptographyConfig config = cryptographyConfig ?? new CryptographyConfig();
        saltLength = config.SaltLength; workFactor = config.NewUserWorkFactor; maxWorkFactor = config.MaxVerificationWorkFactor;
        legacyAlgorithm = config.HashAlgorithm == null ? typeof(SHA256Managed) : config.HashAlgorithm.GetType();
        if (saltLength < 8 || saltLength > 1024) throw new ArgumentOutOfRangeException(nameof(config.SaltLength));
        if (maxWorkFactor < 1 || workFactor < 1 || workFactor > maxWorkFactor) throw new ArgumentOutOfRangeException(nameof(config.NewUserWorkFactor));
    }
    /// <inheritdoc/>
    public CryptographyCredential CreateUserCredentials(string password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));
        byte[] salt = CryptographyUtility.GetRandomBytes(saltLength);
        byte[] bytes = Encoding.UTF8.GetBytes(password);
        try
        {
            Rfc2898DeriveBytes derive = new(bytes, salt, workFactor);
            return new CryptographyCredential { FormatVersion = 1, AlgorithmName = Algorithm,
                LoginSalt = salt, LoginHash = derive.GetBytes(32), LoginWorkFactor = workFactor };
        }
        finally { Array.Clear(bytes, 0, bytes.Length); }
    }
    /// <inheritdoc/>
    public bool VerifyCredentials(CryptographyCredential credential, string password)
    {
        if (credential == null) throw new ArgumentNullException(nameof(credential));
        if (password == null) throw new ArgumentNullException(nameof(password));
        // Snapshot caller-owned mutable DTO fields once, including byte arrays.
        int version = credential.FormatVersion, iterations = credential.LoginWorkFactor;
        string algorithm = credential.AlgorithmName;
        byte[] sourceSalt = credential.LoginSalt, sourceHash = credential.LoginHash;
        if (sourceSalt == null || sourceHash == null || sourceSalt.Length > 1024 || sourceHash.Length > 1024 || iterations > maxWorkFactor) return false;
        byte[] salt = (byte[])sourceSalt.Clone(), expected = (byte[])sourceHash.Clone();
        byte[] bytes;
        if (version == 1)
        {
            if (algorithm != Algorithm || salt.Length < 8 || expected.Length != 32 || iterations < 1) return false;
            bytes = Encoding.UTF8.GetBytes(password);
        }
        else if (version == 0)
        {
            if (salt.Length != HashAlgorithmProvider.SaltLength || expected.Length == 0 ||
                (algorithm != null && algorithm != legacyAlgorithm.FullName)) return false;
            bytes = Encoding.ASCII.GetBytes(password);
            iterations = Math.Max(iterations, 1);
        }
        else return false;
        byte[] actual = null;
        try
        {
            if (version == 1) { Rfc2898DeriveBytes derive = new(bytes, salt, iterations); actual = derive.GetBytes(32); }
            else
            {
                LegacyHasher hasher = new(legacyAlgorithm); actual = bytes;
                for (int i = 0; i < iterations; i++)
                {
                    byte[] next = hasher.Hash(actual, salt);
                    if (!ReferenceEquals(actual, bytes)) Array.Clear(actual, 0, actual.Length);
                    actual = next;
                }
            }
            if (actual.Length != expected.Length) return false;
            int difference = 0;
            for (int i = 0; i < actual.Length; i++) difference |= actual[i] ^ expected[i];
            return difference == 0;
        }
        finally { Array.Clear(bytes, 0, bytes.Length); if (actual != null) Array.Clear(actual, 0, actual.Length); }
    }
    /// <summary>Whether successful authentication should be followed by creating and persisting fresh credentials.</summary>
    public bool NeedsUpgrade(CryptographyCredential credential)
    {
        if (credential == null) throw new ArgumentNullException(nameof(credential));
        return credential.FormatVersion != 1 || credential.AlgorithmName != Algorithm || credential.LoginWorkFactor < workFactor;
    }
    private sealed class LegacyHasher : HashAlgorithmProvider
    {
        internal LegacyHasher(Type algorithm) : base(algorithm, true) { }
        internal byte[] Hash(byte[] bytes, byte[] salt) => CreateHashWithSalt(bytes, salt);
    }
}
