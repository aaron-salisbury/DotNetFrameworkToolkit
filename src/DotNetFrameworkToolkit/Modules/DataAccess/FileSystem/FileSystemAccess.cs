using DotNetFrameworkToolkit.Core;
using DotNetFrameworkToolkit.Modules.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DotNetFrameworkToolkit.Modules.DataAccess.FileSystem;

/// <summary>
/// Provides utility methods for interacting with an operating system's files and directories.
/// </summary>
public sealed class FileSystemAccess : IFileSystemAccess
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemAccess"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger used to record informational messages, warnings, and errors related to file system operations.</param>
    public FileSystemAccess(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                    try { _logger.LogInformation("Removed read-only attribute from file: {FilePath}", fullFilePath); }
                    catch (Exception) { /* Logging must not prevent the deletion. */ }
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
        if (contentLines == null)
        {
            throw new ArgumentNullException(nameof(contentLines));
        }

        if (string.IsNullOrEmpty(fileName) || fileName != Path.GetFileName(fileName))
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
                File.Replace(temporaryPath, fullPath, null);
            }
            else
            {
                File.Move(temporaryPath, fullPath);
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
                catch (Exception) { /* Preserve the write failure. */ }
            }
        }
    }

    /// <inheritdoc/>
    public ProcessResult<string> GetEmbeddedResourceText(Assembly assemblyEmbeddedIn, string filePath)
    {
        if (assemblyEmbeddedIn == null)
        {
            throw new ArgumentNullException(nameof(assemblyEmbeddedIn));
        }

        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("A resource name is required.", nameof(filePath));
        }

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
