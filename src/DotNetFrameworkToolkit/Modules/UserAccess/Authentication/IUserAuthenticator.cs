using System;

namespace DotNetFrameworkToolkit.Modules.UserAccess;

/// <summary>
/// Defines methods for user authentication and credential management,
/// including secure credential creation and password verification.
/// </summary>
public interface IUserAuthenticator
{
    /// <summary>
    /// Creates a new <see cref="CryptographyCredential"/> instance for an application user,
    /// generating a random salt and hash for the provided password.
    /// </summary>
    /// <param name="password">The password to use for credential creation.</param>
    /// <returns>
    /// A new <see cref="CryptographyCredential"/> object containing the generated salt, hash, and work factor.
    /// </returns>
    public CryptographyCredential CreateUserCredentials(string password);

    /// <summary>
    /// Verifies a user's password against the stored credentials.
    /// </summary>
    /// <param name="credential">The user's stored credentials.</param>
    /// <param name="password">The password to verify.</param>
    /// <returns>
    /// <c>true</c> if the password is valid; otherwise, <c>false</c>.
    /// </returns>
    public bool VerifyCredentials(CryptographyCredential credential, string password);
}
