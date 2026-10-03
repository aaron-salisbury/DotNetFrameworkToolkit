using DotNetFrameworkToolkit.Core;
using DotNetFrameworkToolkit.Modules.FileSystem;
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
    public void UnsupportedReplacementPublishesNewContentAndRemovesBackup()
    {
        FileSystemAccess files = new(_logger, UnsupportedReplace, File.Move);
        string path = Path.Combine(_directory, "content.txt");
        File.WriteAllText(path, "original");

        Assert.IsTrue(files.WriteFile(["replacement"], "content.txt", _directory).Value);
        CollectionAssert.AreEqual(new[] { "replacement" }, File.ReadAllLines(path));
        Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
    }

    [TestMethod]
    public void FailedFallbackPublicationRestoresOriginalAndPreservesWriteError()
    {
        IOException writeError = new("Publication failed.");
        int moves = 0;
        FileSystemAccess files = new(_logger, UnsupportedReplace, (source, destination) =>
        {
            moves++;
            if (moves == 2)
            {
                throw writeError;
            }
            File.Move(source, destination);
        });
        string path = Path.Combine(_directory, "content.txt");
        File.WriteAllText(path, "original");

        ProcessResult<bool> result = files.WriteFile(["replacement"], "content.txt", _directory);

        Assert.IsFalse(result.IsSuccessful);
        Assert.AreSame(writeError, result.Error.InnerException);
        Assert.AreEqual("original", File.ReadAllText(path));
        Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
    }

    [TestMethod]
    public void FailedBackupMoveLeavesOriginalUntouched()
    {
        IOException moveError = new("Backup move failed.");
        FileSystemAccess files = new(_logger, UnsupportedReplace, (source, destination) =>
        {
            throw moveError;
        });
        string path = Path.Combine(_directory, "content.txt");
        File.WriteAllText(path, "original");

        ProcessResult<bool> result = files.WriteFile(["replacement"], "content.txt", _directory);

        Assert.IsFalse(result.IsSuccessful);
        Assert.AreSame(moveError, result.Error.InnerException);
        Assert.AreEqual("original", File.ReadAllText(path));
        Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
    }

    [TestMethod]
    public void FailedFallbackRestorationRetainsOriginalBackupAndBothErrors()
    {
        IOException writeError = new("Publication failed.");
        IOException restoreError = new("Restoration failed.");
        int moves = 0;
        FileSystemAccess files = new(_logger, UnsupportedReplace, (source, destination) =>
        {
            moves++;
            if (moves == 2)
            {
                throw writeError;
            }
            if (moves == 3)
            {
                throw restoreError;
            }
            File.Move(source, destination);
        });
        string path = Path.Combine(_directory, "content.txt");
        File.WriteAllText(path, "original");

        ProcessResult<bool> result = files.WriteFile(["replacement"], "content.txt", _directory);

        Assert.IsFalse(result.IsSuccessful);
        DotNetFrameworkToolkit.Core.AggregateException error = (DotNetFrameworkToolkit.Core.AggregateException)result.Error.InnerException;
        Assert.AreEqual(2, error.InnerExceptions.Count);
        Assert.AreSame(writeError, error.InnerExceptions[0]);
        Assert.AreSame(restoreError, error.InnerExceptions[1]);
        string[] backups = Directory.GetFiles(_directory, "*.bak");
        Assert.AreEqual(1, backups.Length);
        Assert.AreEqual("original", File.ReadAllText(backups[0]));
        StringAssert.Contains(error.Message, backups[0]);
        Assert.IsFalse(File.Exists(path));
        Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
    }

    [TestMethod]
    public void OrdinaryReplacementErrorDoesNotAttemptFallback()
    {
        IOException replaceError = new("Sharing violation.");
        int moves = 0;
        FileSystemAccess files = new(_logger, (source, destination, backup) =>
        {
            throw replaceError;
        }, (source, destination) =>
        {
            moves++;
            File.Move(source, destination);
        });
        string path = Path.Combine(_directory, "content.txt");
        File.WriteAllText(path, "original");

        ProcessResult<bool> result = files.WriteFile(["replacement"], "content.txt", _directory);

        Assert.IsFalse(result.IsSuccessful);
        Assert.AreSame(replaceError, result.Error.InnerException);
        Assert.AreEqual(0, moves);
        Assert.AreEqual("original", File.ReadAllText(path));
        Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
    }

    [TestMethod]
    public void BackupCleanupAndLoggingFailuresDoNotReverseCommittedWrite()
    {
        string backup = null;
        int moves = 0;
        FileSystemAccess files = new(_logger, UnsupportedReplace, (source, destination) =>
        {
            moves++;
            File.Move(source, destination);
            if (moves == 1)
            {
                backup = destination;
            }
            else if (moves == 2)
            {
                File.SetAttributes(backup, FileAttributes.ReadOnly);
            }
        });
        string path = Path.Combine(_directory, "content.txt");
        File.WriteAllText(path, "original");
        _logger.Dispose();

        ProcessResult<bool> result = files.WriteFile(["replacement"], "content.txt", _directory);

        Assert.IsTrue(result.IsSuccessful);
        CollectionAssert.AreEqual(new[] { "replacement" }, File.ReadAllLines(path));
        Assert.AreEqual("original", File.ReadAllText(backup));
        Assert.AreEqual(2, Directory.GetFiles(_directory).Length);
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

    private static void UnsupportedReplace(string source, string destination, string backup)
    {
        throw new PlatformNotSupportedException("File replacement is unavailable.");
    }

    private static IEnumerable<string> ThrowingLines()
    {
        yield return "partial";
        throw new ApplicationException("enumeration failed");
    }
}
