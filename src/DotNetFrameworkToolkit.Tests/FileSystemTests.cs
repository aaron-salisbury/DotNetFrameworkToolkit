using DotNetFrameworkToolkit.Core;
using DotNetFrameworkToolkit.Modules.DataAccess.FileSystem;
using DotNetFrameworkToolkit.Modules.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class FileSystemTests
{
    private string _directory;
    private LoggerPNP _logger;
    private FileSystemAccess _files;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "ToolkitTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _logger = new LoggerPNP(LogLevel.Information, new InMemorySinkPNP());
        _files = new FileSystemAccess(_logger);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _logger.Dispose();
        foreach (string file in Directory.GetFiles(_directory))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
        Directory.Delete(_directory, true);
    }

    [TestMethod]
    public void MissingDeleteIsSuccessfulButContainsFalse()
    {
        ProcessResult<bool> result = _files.DeleteFile(Path.Combine(_directory, "missing.txt"));
        Assert.IsTrue(result.IsSuccessful);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void ReadOnlyDeleteWorksWithEnabledNamedLogging()
    {
        string path = Path.Combine(_directory, "readonly.txt");
        File.WriteAllText(path, "content");
        File.SetAttributes(path, FileAttributes.ReadOnly);
        Assert.IsTrue(_files.DeleteFile(path).Value);
        Assert.IsFalse(File.Exists(path));
    }

    [TestMethod]
    public void WriteCreatesThenAtomicallyReplacesExistingFile()
    {
        Assert.IsTrue(_files.WriteFile(["first", "second"], "content.txt", _directory).Value);
        Assert.IsTrue(_files.WriteFile(["replacement"], "content.txt", _directory).Value);
        CollectionAssert.AreEqual(new[] { "replacement" }, File.ReadAllLines(Path.Combine(_directory, "content.txt")));
        Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
    }

    [TestMethod]
    public void FailedEnumerationPreservesOriginalAndCleansTemporaryFile()
    {
        string path = Path.Combine(_directory, "content.txt");
        File.WriteAllText(path, "original");
        Assert.IsFalse(_files.WriteFile(ThrowingLines(), "content.txt", _directory).IsSuccessful);
        Assert.AreEqual("original", File.ReadAllText(path));
        Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
    }

    [TestMethod]
    public void FailedNewWriteDoesNotPublishPartialFile()
    {
        Assert.IsFalse(_files.WriteFile(ThrowingLines(), "content.txt", _directory).IsSuccessful);
        Assert.AreEqual(0, Directory.GetFiles(_directory).Length);
    }

    [TestMethod]
    public void PathsAndNullContentAreRejected()
    {
        Assert.ThrowsException<ArgumentException>(() => _files.WriteFile(new[] { "content" }, "../escape.txt", _directory));
        Assert.ThrowsException<ArgumentNullException>(() => _files.WriteFile(null, "content.txt", _directory));
    }

    [TestMethod]
    public void EmbeddedTextIsReadAndMissingResourceReturnsDiagnostic()
    {
        Assembly assembly = typeof(FileSystemTests).Assembly;
        Assert.AreEqual("embedded fixture", _files.GetEmbeddedResourceText(assembly, "DotNetFrameworkToolkit.Tests.Fixture.txt").Value.Trim());
        ProcessResult<string> missing = _files.GetEmbeddedResourceText(assembly, "missing.resource");
        Assert.IsFalse(missing.IsSuccessful);
        StringAssert.Contains(missing.Error.ToString(), "missing.resource");
    }

    private static IEnumerable<string> ThrowingLines()
    {
        yield return "partial";
        throw new ApplicationException("enumeration failed");
    }
}
