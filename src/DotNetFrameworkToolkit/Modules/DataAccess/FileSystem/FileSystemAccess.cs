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
        _logger = logger;
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
        try
        {
            if (File.Exists(fullFilePath))
            {
                FileAttributes attributes = File.GetAttributes(fullFilePath);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    // Remove read-only attribute before deleting.
                    File.SetAttributes(fullFilePath, attributes & ~FileAttributes.ReadOnly);
                    _logger?.LogInformation("Removed read-only attribute from file: {FilePath}", fullFilePath);
                }

                File.Delete(fullFilePath);
                return ProcessResult<bool>.Success(true);
            }
            else
            {
                return ProcessResult<bool>.LogAndForwardException(
                    "Nothing to delete.", 
                    new FileNotFoundException("Attempted to delete a file that does not exist.", fullFilePath), 
                    _logger, 
                    LogLevel.Warning);
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

            using StreamWriter outputFile = new(fullPath);
            foreach (string line in contentLines)
            {
                outputFile.WriteLine(line);
            }

            return ProcessResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ProcessResult<bool>.LogAndForwardException("Failed to write file.", ex, _logger);
        }
    }

    /// <inheritdoc/>
    public ProcessResult<string> GetEmbeddedResourceText(Assembly assemblyEmbeddedIn, string filePath)
    {
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
