using DotNetFrameworkToolkit.Modules.UserAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Cryptography;
using System.Text;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class AuthenticationTests
{
    [DataTestMethod]
    [DataRow("password")]
    [DataRow("pässword")]
    [DataRow("密码🔐")]
    [DataRow("")]
    public void CredentialsRoundTripAndRejectOtherPasswords(string password)
    {
        UserAuthenticator authenticator = CreateAuthenticator();
        CryptographyCredential credential = authenticator.CreateUserCredentials(password);
        Assert.IsTrue(authenticator.VerifyCredentials(credential, password));
        Assert.IsFalse(authenticator.VerifyCredentials(credential, password + "wrong"));
        Assert.AreEqual(32, credential.LoginHash.Length);
    }

    [TestMethod]
    public void UnicodePasswordDoesNotAliasAsciiQuestionMark()
    {
        UserAuthenticator authenticator = CreateAuthenticator();
        CryptographyCredential credential = authenticator.CreateUserCredentials("pässword");
        Assert.IsFalse(authenticator.VerifyCredentials(credential, "p?ssword"));
    }

    [TestMethod]
    public void CredentialsUseIndependentRandomSaltsAndStandardPbkdf2()
    {
        UserAuthenticator authenticator = CreateAuthenticator();
        CryptographyCredential first = authenticator.CreateUserCredentials("password");
        CryptographyCredential second = authenticator.CreateUserCredentials("password");
        Assert.IsFalse(Convert.ToBase64String(first.LoginSalt) == Convert.ToBase64String(second.LoginSalt));
        using Rfc2898DeriveBytes reference = new(Encoding.UTF8.GetBytes("password"), first.LoginSalt, first.LoginWorkFactor);
        CollectionAssert.AreEqual(reference.GetBytes(32), first.LoginHash);
    }

    [TestMethod]
    public void ConfigurationIsSnapshotted()
    {
        CryptographyConfig config = new() { SaltLength = 16, NewUserWorkFactor = 10, MaxVerificationWorkFactor = 100 };
        UserAuthenticator authenticator = new(config);
        config.SaltLength = 1;
        config.NewUserWorkFactor = 0;
        config.MaxVerificationWorkFactor = 0;
        CryptographyCredential credential = authenticator.CreateUserCredentials("password");
        Assert.AreEqual(16, credential.LoginSalt.Length);
        Assert.AreEqual(10, credential.LoginWorkFactor);
        Assert.IsTrue(authenticator.VerifyCredentials(credential, "password"));
    }

    [DataTestMethod]
    [DataRow(7, 10, 100)]
    [DataRow(1025, 10, 100)]
    [DataRow(16, 0, 100)]
    [DataRow(16, 101, 100)]
    [DataRow(16, 10, 0)]
    public void InvalidConfigurationIsRejected(int salt, int work, int maximum)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new UserAuthenticator(new CryptographyConfig { SaltLength = salt, NewUserWorkFactor = work, MaxVerificationWorkFactor = maximum }));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(101)]
    public void InvalidStoredWorkFactorsAreRejected(int iterations)
    {
        UserAuthenticator authenticator = CreateAuthenticator();
        CryptographyCredential credential = authenticator.CreateUserCredentials("password");
        credential.LoginWorkFactor = iterations;
        Assert.IsFalse(authenticator.VerifyCredentials(credential, "password"));
    }

    [TestMethod]
    public void MalformedStoredArraysAreRejected()
    {
        UserAuthenticator authenticator = CreateAuthenticator();
        Assert.IsFalse(authenticator.VerifyCredentials(new CryptographyCredential(), "password"));
        Assert.IsFalse(authenticator.VerifyCredentials(new CryptographyCredential { LoginSalt = new byte[7], LoginHash = new byte[32], LoginWorkFactor = 10 }, "password"));
        Assert.IsFalse(authenticator.VerifyCredentials(new CryptographyCredential { LoginSalt = new byte[16], LoginHash = new byte[31], LoginWorkFactor = 10 }, "password"));
        Assert.IsFalse(authenticator.VerifyCredentials(new CryptographyCredential { LoginSalt = new byte[1025], LoginHash = new byte[32], LoginWorkFactor = 10 }, "password"));
    }

    [TestMethod]
    public void NullInputsAreRejected()
    {
        UserAuthenticator authenticator = CreateAuthenticator();
        Assert.ThrowsException<ArgumentNullException>(() => authenticator.CreateUserCredentials(null));
        Assert.ThrowsException<ArgumentNullException>(() => authenticator.VerifyCredentials(null, "password"));
        Assert.ThrowsException<ArgumentNullException>(() => authenticator.VerifyCredentials(new CryptographyCredential(), null));
    }

    private static UserAuthenticator CreateAuthenticator()
    {
        return new UserAuthenticator(new CryptographyConfig { SaltLength = 16, NewUserWorkFactor = 10, MaxVerificationWorkFactor = 100 });
    }
}
