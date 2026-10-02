using DotNetFrameworkToolkit.Core;
using DotNetFrameworkToolkit.Modules.ComponentModel;
using DotNetFrameworkToolkit.Modules.Logging;
using DotNetFrameworkToolkit.Modules.UserAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class PublicContractTests
{
    public enum WideError : ulong
    {
        Success = 0,
        Failure = ulong.MaxValue
    }

    [TestMethod]
    public void ExceptionCanBeASuccessfulValueWithoutBecomingAnError()
    {
        Exception value = new ApplicationException("data");
        ProcessResult<Exception> result = ProcessResult<Exception>.Success(value);
        Assert.IsTrue(result);
        Assert.AreSame(value, result.Value);
        Assert.IsNull(result.Error);
        Assert.IsTrue(result.TryGet(out Exception extracted));
        Assert.AreSame(value, extracted);

        ProcessResult<Exception> nullResult = ProcessResult<Exception>.Success(null);
        Assert.IsTrue(nullResult);
        Assert.IsNull(nullResult.Value);
    }

    [TestMethod]
    public void EnumResultsSupportWideUnderlyingValuesAndSuccessfulNulls()
    {
        ProcessResult<string, WideError> success = ProcessResult<string, WideError>.Success(null);
        Assert.IsTrue(success);
        Assert.IsNull(success.Value);
        Assert.AreEqual(WideError.Success, success.Error);

        ProcessResult<string, WideError> failure = ProcessResult<string, WideError>.Failure(WideError.Failure);
        Assert.IsFalse(failure);
        Assert.AreEqual(WideError.Failure, failure.Error);
        Assert.IsNull(failure.ValueOrDefault);
        Assert.IsFalse(failure.TryGet(out string value));
        Assert.IsNull(value);
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            string ignored = failure.Value;
        });
    }

    [TestMethod]
    public void NonzeroUndefinedEnumErrorsArePreserved()
    {
        WideError unknown = (WideError)42;
        ProcessResult<int, WideError> result = ProcessResult<int, WideError>.Failure(unknown);
        Assert.IsFalse(result);
        Assert.AreEqual(unknown, result.Error);
        Assert.AreEqual(0, result.ValueOrDefault);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ProcessResult<int, WideError>.Failure(default));
    }

    [TestMethod]
    public void ForwardingPreservesOperationFailureWhenLoggerIsDisposed()
    {
        LoggerPNP logger = new(LogLevel.Error);
        logger.Dispose();
        Exception original = new ApplicationException("operation");
        ProcessResult<int> result = ProcessResult<int>.LogAndForwardException("context", original, logger);
        Assert.AreSame(original, result.Error.InnerException);
        Assert.IsInstanceOfType(result.Error.Data["LoggingException"], typeof(ObjectDisposedException));
        Assert.AreEqual("context", result.Error.Message);
    }

    [TestMethod]
    public void ForwardingRequiresBothOriginalExceptionAndLogger()
    {
        using (LoggerPNP logger = new(LogLevel.None))
        {
            Assert.ThrowsException<ArgumentNullException>(() => ProcessResult<int>.LogAndForwardException("context", null, logger));
        }
        Assert.ThrowsException<ArgumentNullException>(() => ProcessResult<int>.LogAndForwardException("context", new Exception(), null));
    }

    [TestMethod]
    public void ValidationResultTreatsAnyRetainedMessageAsInvalid()
    {
        ValidationResult<int> properties = new(7, new Dictionary<string, string[]>
        {
            { "IgnoredNull", null },
            { "IgnoredEmpty", new string[0] },
            { "Retained", new[] { string.Empty } }
        }, null);
        Assert.IsFalse(properties.IsValid);
        Assert.AreEqual("Retained", properties.ValidationMessages.Single().Key);

        ValidationResult<int> general = new(7, null, new[] { string.Empty });
        Assert.IsFalse(general.IsValid);
        Assert.AreEqual(string.Empty, general.GeneralMessages.Single());
        Assert.IsTrue(new ValidationResult<int>(7, null, null).IsValid);
    }

    [TestMethod]
    public void ValidatorNotificationsFollowOrderedMessageChanges()
    {
        ContractValidator model = new();
        List<string> notifications = new();
        model.ErrorsChangedCore += (sender, args) =>
        {
            notifications.Add("errors:" + args.PropertyName);
        };
        model.PropertyChanged += (sender, args) =>
        {
            notifications.Add("property:" + args.PropertyName);
        };
        model.SetProperty("first", "second");
        model.SetProperty("first", "second");
        model.SetProperty("second", "first");
        model.SetProperty();
        CollectionAssert.AreEqual(new[]
        {
            "errors:Name", "property:HasErrors",
            "errors:Name", "property:HasErrors",
            "errors:Name", "property:HasErrors"
        }, notifications);
        Assert.IsFalse(model.HasErrors);
    }

    [TestMethod]
    public void ValidatorCopiesErrorsAndSnapshotsRemainStable()
    {
        ContractValidator model = new();
        List<string> errors = new() { "first" };
        model.SetPropertyList(errors);
        errors[0] = "mutated";
        List<string> snapshot = model.GetErrorsForProperty(nameof(ContractValidator.Name));
        model.SetProperty("second");
        CollectionAssert.AreEqual(new[] { "first" }, snapshot);
        CollectionAssert.AreEqual(new[] { "second" }, model.GetErrorsForProperty(nameof(ContractValidator.Name)));

        string[] entity = { "entity" };
        model.SetEntity(entity);
        entity[0] = "mutated";
        string[] entitySnapshot = model.GetErrors(null).Cast<string>().ToArray();
        model.SetEntity("replacement");
        CollectionAssert.AreEqual(new[] { "entity" }, entitySnapshot);
        Assert.AreEqual("replacement", model.Error);
    }

    [TestMethod]
    public void ValidationObserversSeeCommittedStateAndExceptionsPropagate()
    {
        ContractValidator model = new();
        ApplicationException observerError = new("observer");
        bool sawCommittedState = false;
        model.ErrorsChangedCore += (sender, args) =>
        {
            sawCommittedState = model.HasErrors && model.GetErrorsForProperty(args.PropertyName).Single() == "required";
            throw observerError;
        };
        ApplicationException actual = Assert.ThrowsException<ApplicationException>(() => model.SetProperty("required"));
        Assert.AreSame(observerError, actual);
        Assert.IsTrue(sawCommittedState);
        Assert.IsTrue(model.HasErrors);
        // Identical errors do not notify again, even if the previous observer threw.
        model.SetProperty("required");
    }

    [TestMethod]
    public void ValidationAndPropertyNotificationOrderIsExplicit()
    {
        ContractValidator model = new();
        List<string> notifications = new();
        model.ErrorsChangedCore += (sender, args) =>
        {
            notifications.Add("errors:" + args.PropertyName);
        };
        model.PropertyChanged += (sender, args) =>
        {
            notifications.Add("property:" + args.PropertyName);
        };
        model.RaisePropertyChangedWithValidation(nameof(ContractValidator.Name));
        model.RaisePropertyChangedWithValidation(nameof(ContractValidator.Name));
        CollectionAssert.AreEqual(new[]
        {
            "errors:Name", "property:HasErrors", "property:Name", "property:Name"
        }, notifications);
    }

    [TestMethod]
    public void InvalidValidationReturnLeavesPreviousErrorsIntact()
    {
        ContractValidator model = new();
        model.SetProperty("previous");
        model.ReturnNull = true;
        Assert.ThrowsException<ArgumentNullException>(() => model.PropertyIsValid(nameof(ContractValidator.Name)));
        CollectionAssert.AreEqual(new[] { "previous" }, model.GetErrorsForProperty(nameof(ContractValidator.Name)));
    }

    [TestMethod]
    public void EntityNotificationsIncludeErrorAndHasErrorsEvenWhilePropertyErrorsRemain()
    {
        ContractValidator model = new();
        model.SetProperty("property");
        List<string> notifications = new();
        model.ErrorsChangedCore += (sender, args) =>
        {
            notifications.Add("errors:" + args.PropertyName);
        };
        model.PropertyChanged += (sender, args) =>
        {
            notifications.Add("property:" + args.PropertyName);
        };
        model.SetEntity("entity");
        model.SetEntity("entity");
        model.SetEntity();
        CollectionAssert.AreEqual(new[]
        {
            "errors:", "property:Error", "property:HasErrors",
            "errors:", "property:Error", "property:HasErrors"
        }, notifications);
        Assert.IsTrue(model.HasErrors);
        Assert.AreEqual(string.Empty, model.Error);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("missing")]
    public void ValidationRequiresARealPropertyName(string name)
    {
        Assert.ThrowsException<ArgumentException>(() => new ContractValidator().PropertyIsValid(name));
    }

    [TestMethod]
    public void PersistedCredentialVerifiesAcrossAuthenticatorConfigurationChanges()
    {
        UserAuthenticator writer = new(new CryptographyConfig
        {
            SaltLength = 8,
            NewUserWorkFactor = 10,
            MaxVerificationWorkFactor = 100
        });
        CryptographyCredential original = writer.CreateUserCredentials("pässword");
        // Simulate storage without relying on a particular serializer or database.
        string salt = Convert.ToBase64String(original.LoginSalt);
        string hash = Convert.ToBase64String(original.LoginHash);
        CryptographyCredential restored = new()
        {
            LoginSalt = Convert.FromBase64String(salt),
            LoginHash = Convert.FromBase64String(hash),
            LoginWorkFactor = original.LoginWorkFactor
        };
        UserAuthenticator reader = new(new CryptographyConfig
        {
            SaltLength = 32,
            NewUserWorkFactor = 20,
            MaxVerificationWorkFactor = 100
        });
        Assert.IsTrue(reader.VerifyCredentials(restored, "pässword"));
        Assert.AreEqual(salt, Convert.ToBase64String(restored.LoginSalt));
        Assert.AreEqual(hash, Convert.ToBase64String(restored.LoginHash));
        Assert.AreEqual(10, restored.LoginWorkFactor);
    }

    [TestMethod]
    public void LowerVerificationCeilingRejectsStoredCredentialWithoutRewritingIt()
    {
        UserAuthenticator writer = new(new CryptographyConfig
        {
            SaltLength = 16,
            NewUserWorkFactor = 20,
            MaxVerificationWorkFactor = 100
        });
        CryptographyCredential credential = writer.CreateUserCredentials("password");
        byte[] hash = (byte[])credential.LoginHash.Clone();
        UserAuthenticator reader = new(new CryptographyConfig
        {
            SaltLength = 16,
            NewUserWorkFactor = 10,
            MaxVerificationWorkFactor = 10
        });
        Assert.IsFalse(reader.VerifyCredentials(credential, "password"));
        CollectionAssert.AreEqual(hash, credential.LoginHash);
        Assert.AreEqual(20, credential.LoginWorkFactor);
    }

    [TestMethod]
    public void CredentialTamperingIsRejectedWithoutModifyingStoredArrays()
    {
        UserAuthenticator authenticator = new(new CryptographyConfig
        {
            SaltLength = 16,
            NewUserWorkFactor = 10,
            MaxVerificationWorkFactor = 100
        });
        CryptographyCredential credential = authenticator.CreateUserCredentials("password");
        credential.LoginHash[31] ^= 1;
        byte[] tampered = (byte[])credential.LoginHash.Clone();
        Assert.IsFalse(authenticator.VerifyCredentials(credential, "password"));
        CollectionAssert.AreEqual(tampered, credential.LoginHash);
    }

    [DataTestMethod]
    [DataRow("password", "Y/wc4wZJZD4E2Zer7XvvRO45YQXTFBdgIEkYdndO1XE=")]
    [DataRow("pässword", "k6gyLSQs2+RzuXcEy7pCzShJRJV4gMLvoC3f6MFG7jA=")]
    public void UnversionedCredentialFormatMatchesFixedPbkdf2Sha1Vectors(string password, string hash)
    {
        // Independently generated with Python hashlib.pbkdf2_hmac("sha1", UTF8, salt, 10, 32).
        // These vectors lock down persistence, rather than just round-tripping the implementation.
        byte[] salt = new byte[16];
        for (int i = 0; i < salt.Length; i++)
        {
            salt[i] = (byte)i;
        }
        CryptographyCredential stored = new()
        {
            LoginSalt = salt,
            LoginHash = Convert.FromBase64String(hash),
            LoginWorkFactor = 10
        };
        UserAuthenticator authenticator = new(new CryptographyConfig
        {
            SaltLength = 16,
            NewUserWorkFactor = 20,
            MaxVerificationWorkFactor = 100
        });
        Assert.IsTrue(authenticator.VerifyCredentials(stored, password));
        Assert.IsFalse(authenticator.VerifyCredentials(stored, password + "wrong"));
    }

    private sealed class ContractValidator : ObservableValidator
    {
        public string Name { get; set; }
        public bool ReturnNull { get; set; }

        public override List<string> ValidateProperty(PropertyDescriptor property)
        {
            if (ReturnNull)
            {
                return null;
            }
            if (property.Name == nameof(Name) && string.IsNullOrEmpty(Name))
            {
                return new List<string> { "required" };
            }
            return new List<string>();
        }

        public void SetProperty(params string[] errors)
        {
            SetPropertyList(new List<string>(errors));
        }

        public void SetPropertyList(List<string> errors)
        {
            SetErrorsForProperty(nameof(Name), errors);
        }

        public void SetEntity(params string[] errors)
        {
            SetEntityLevelErrors(errors);
        }
    }
}
