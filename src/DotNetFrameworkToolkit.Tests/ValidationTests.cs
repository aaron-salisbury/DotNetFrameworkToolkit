using DotNetFrameworkToolkit.Modules.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class ValidationTests
{
    [TestMethod]
    public void FullValidationFindsNewErrorsAndClearsCorrectedErrors()
    {
        Validator model = new();
        Assert.IsFalse(model.IsValid());
        Assert.IsTrue(model.HasErrors);
        model.Name = "valid";
        Assert.IsTrue(model.IsValid());
        Assert.IsFalse(model.HasErrors);
    }

    [TestMethod]
    public void EntityErrorsAffectValidityAndSupportNullOrEmptyQueries()
    {
        Validator model = new() { Name = "valid" };
        model.SetEntity("entity");
        Assert.IsFalse(model.IsValid());
        Assert.AreEqual("entity", model.Error);
        CollectionAssert.AreEqual(new[] { "entity" }, model.GetErrors(null).Cast<string>().ToArray());
        CollectionAssert.AreEqual(new[] { "entity" }, model.GetErrors(string.Empty).Cast<string>().ToArray());
        model.SetEntity();
        Assert.IsTrue(model.IsValid());
    }

    [TestMethod]
    public void PropertyNotificationsOccurOnlyWhenErrorsChange()
    {
        Validator model = new();
        List<string> notifications = new();
        model.ErrorsChangedCore += (sender, args) => notifications.Add(args.PropertyName);
        Assert.IsFalse(model.PropertyIsValid(nameof(Validator.Name)));
        Assert.IsFalse(model.PropertyIsValid(nameof(Validator.Name)));
        Assert.AreEqual(1, notifications.Count);
        model.Name = "valid";
        Assert.IsTrue(model.PropertyIsValid(nameof(Validator.Name)));
        Assert.AreEqual(2, notifications.Count);
        CollectionAssert.AreEqual(new[] { "Name", "Name" }, notifications);
    }

    [TestMethod]
    public void ErrorSnapshotsCannotMutateModel()
    {
        Validator model = new();
        model.IsValid();
        model.GetErrorsForProperty(nameof(Validator.Name)).Clear();
        Assert.IsTrue(model.HasErrors);
        Assert.AreEqual(1, model.GetErrorsForProperty(nameof(Validator.Name)).Count);
    }

    [TestMethod]
    public void EntityChangesNotifyErrorAndHasErrorsWithoutDuplicateNotifications()
    {
        Validator model = new();
        List<string> properties = new();
        model.PropertyChanged += (sender, args) => properties.Add(args.PropertyName);
        model.SetEntity("entity");
        model.SetEntity("entity");
        CollectionAssert.AreEqual(new[] { "Error", "HasErrors" }, properties);
    }

    [TestMethod]
    public void UnknownPropertyIsRejected()
    {
        Assert.ThrowsException<ArgumentException>(() => new Validator().PropertyIsValid("missing"));
    }

    [TestMethod]
    public void ObservableFieldNotifiesOnlyOnValueChange()
    {
        Observable model = new();
        int changes = 0;
        model.PropertyChanged += (sender, args) => changes++;
        Assert.IsTrue(model.Change("first"));
        Assert.IsFalse(model.Change("first"));
        Assert.IsTrue(model.Change(null));
        Assert.AreEqual(2, changes);
    }

    public sealed class Validator : ObservableValidator
    {
        public string Name { get; set; }
        public override List<string> ValidateProperty(PropertyDescriptor property)
        {
            return property.Name == nameof(Name) && string.IsNullOrEmpty(Name) ? new List<string> { "required" } : new List<string>();
        }
        public void SetEntity(params string[] errors)
        {
            SetEntityLevelErrors(errors);
        }
    }

    private sealed class Observable : ObservableObject
    {
        private string _value;
        public bool Change(string value)
        {
            return SetField(ref _value, value, "Value");
        }
    }
}
