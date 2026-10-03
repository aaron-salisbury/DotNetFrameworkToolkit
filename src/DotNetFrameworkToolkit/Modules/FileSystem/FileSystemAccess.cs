using DotNetFrameworkToolkit.Core;
using DotNetFrameworkToolkit.Modules.Logging;
using Microsoft.Practices.Unity.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DotNetFrameworkToolkit.Modules.FileSystem;

/// <summary>
/// Provides utility methods for interacting with an operating system's files and directories.
/// </summary>
public sealed class FileSystemAccess : IFileSystemAccess
{
    private readonly ILogger _logger;
    private readonly FileReplaceOperation _replace;
    private readonly FileMoveOperation _move;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemAccess"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger used to record informational messages, warnings, and errors related to file system operations.</param>
    public FileSystemAccess(ILogger logger) : this(logger, File.Replace, File.Move)
    {
    }

    internal FileSystemAccess(ILogger logger, FileReplaceOperation replace, FileMoveOperation move)
    {
        Guard.ArgumentNotNull(logger, nameof(logger));
        Guard.ArgumentNotNull(replace, nameof(replace));
        Guard.ArgumentNotNull(move, nameof(move));

        _logger = logger;
        _replace = replace;
        _move = move;
    }

    /// <inheritdoc/>
    public ProcessResult<string> GetAppDirectoryPath()
    {
        try
        {
            string localAppDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string thisAppName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
            string appDirectory = Path.Combine(localAppDirectory, thisAppName);

            Directory.CreateDirectory(appDirectory);

            return ProcessResult<string>.Success(appDirectory);
        }
        catch (Exception ex)
        {
            return ProcessResult<string>.LogAndForwardException("Failed to get or create application directory.", ex, _logger);
        }
    }

    /// <inheritdoc/>
    public ProcessResult<bool> DeleteFile(string fullFilePath)
    {
        if (string.IsNullOrEmpty(fullFilePath))
        {
            throw new ArgumentException("A file path is required.", nameof(fullFilePath));
        }

        try
        {
            if (File.Exists(fullFilePath))
            {
                FileAttributes attributes = File.GetAttributes(fullFilePath);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    // Remove read-only attribute before deleting.
                    File.SetAttributes(fullFilePath, attributes & ~FileAttributes.ReadOnly);
                    try
                    {
                        _logger.LogInformation("Removed read-only attribute from file: {FilePath}", fullFilePath);
                    }
                    catch (Exception)
                    {
                        /* Logging must not prevent the deletion. */
                    }
                }

                File.Delete(fullFilePath);
                return ProcessResult<bool>.Success(true);
            }
            else
            {
                return ProcessResult<bool>.Success(false);
            }
        }
        catch (Exception ex)
        {
            return ProcessResult<bool>.LogAndForwardException($"Failed to delete file: {fullFilePath}", ex, _logger);
        }
    }

    /// <inheritdoc/>
    public ProcessResult<bool> WriteFile(IEnumerable<string> contentLines, string fileName, string directoryPath = null)
    {
        Guard.ArgumentNotNull(contentLines, nameof(contentLines));
        Guard.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

        if (fileName != Path.GetFileName(fileName))
        {
            throw new ArgumentException("A simple file name is required.", nameof(fileName));
        }

        string temporaryPath = null;
        try
        {
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            else
            {
                ProcessResult<string> appDirectoryResult = GetAppDirectoryPath();
                if (!appDirectoryResult.IsSuccessful)
                {
                    return ProcessResult<bool>.LogAndForwardException("Failed to retrieve default directory path.", appDirectoryResult.Error, _logger);
                }

                directoryPath = appDirectoryResult.Value;
            }

            string fullPath = Path.Combine(directoryPath, fileName);

            temporaryPath = Path.Combine(directoryPath, Guid.NewGuid().ToString("N") + ".tmp");
            using (StreamWriter outputFile = new(temporaryPath))
            {
                foreach (string line in contentLines)
                {
                    outputFile.WriteLine(line);
                }
            }

            if (File.Exists(fullPath))
            {
                ReplaceExistingFile(temporaryPath, fullPath);
            }
            else
            {
                _move(temporaryPath, fullPath);
            }

            temporaryPath = null;

            return ProcessResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ProcessResult<bool>.LogAndForwardException("Failed to write file.", ex, _logger);
        }
        finally
        {
            if (temporaryPath != null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception)
                {
                    /* Preserve the write failure. */
                }
            }
        }
    }

    private void ReplaceExistingFile(string temporaryPath, string fullPath)
    {
        try
        {
            _replace(temporaryPath, fullPath, null);
            return;
        }
        catch (PlatformNotSupportedException)
        {
            // The fallback is intentionally limited to unavailable platform support.
            // Sharing violations, access errors and other I/O failures must propagate.
        }

        string backupPath = temporaryPath + ".bak";
        _move(fullPath, backupPath);
        try
        {
            _move(temporaryPath, fullPath);
        }
        catch (Exception writeError)
        {
            try
            {
                _move(backupPath, fullPath);
            }
            catch (Exception restoreError)
            {
                // Keep the backup for recovery; never delete the last original copy.
                throw new Core.AggregateException("Writing and restoring the file failed. The original remains at: " + backupPath, writeError, restoreError);
            }
            throw;
        }

        try
        {
            File.Delete(backupPath);
        }
        catch (Exception cleanupError)
        {
            // The new file was committed. A leftover backup does not undo that success.
            try
            {
                _logger.LogWarning(cleanupError, "File written, but its original backup could not be removed: {BackupPath}", backupPath);
            }
            catch (Exception)
            {
                /* Logging must not turn a committed write into a reported failure. */
            }
        }
    }

    /// <inheritdoc/>
    public ProcessResult<string> GetEmbeddedResourceText(Assembly assemblyEmbeddedIn, string filePath)
    {
        Guard.ArgumentNotNull(assemblyEmbeddedIn, nameof(assemblyEmbeddedIn));
        Guard.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

        try
        {
            using Stream stream = assemblyEmbeddedIn.GetManifestResourceStream(filePath);

            if (stream is null)
            {
                return ProcessResult<string>.LogAndForwardException(
                $"Embedded resource '{filePath}' not found in assembly '{assemblyEmbeddedIn.FullName}'.",
                new FileNotFoundException($"The specified embedded resource '{filePath}' could not be found."),
                _logger);
            }

            using StreamReader streamReader = new(stream);

            return ProcessResult<string>.Success(streamReader.ReadToEnd());
        }
        catch (Exception ex)
        {
            return ProcessResult<string>.LogAndForwardException("Failed to retrieve embedded text.", ex, _logger);
        }
    }
}

// Internal fault-injection seams exercise filesystem failure paths with real files.
internal delegate void FileReplaceOperation(string source, string destination, string backup);
internal delegate void FileMoveOperation(string source, string destination);
