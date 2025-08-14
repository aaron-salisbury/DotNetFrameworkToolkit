using DotNetFrameworkToolkit.Core;
using System.Collections.Generic;
using System.Reflection;

namespace DotNetFrameworkToolkit.Modules.DataAccess.FileSystem;

/// <summary>
/// Provides utility methods for interacting with an operating system's files and directories.
/// </summary>
public interface IFileSystemAccess
{
    /// <summary>
    /// Gets the path to the application's local data directory, creating it if it does not exist.
    /// </summary>
    /// <returns>
    /// A <see cref="ProcessResult{T}"/> whose <c>Value</c> property contains the full path to the application's local data directory.
    /// </returns>
    ProcessResult<string> GetAppDirectoryPath();

    /// <summary>
    /// Deletes the specified file if it exists, handling read-only files and logging warnings or errors as appropriate.
    /// </summary>
    /// <param name="fullFilePath">The full path of the file to delete.</param>
    /// <returns>
    /// A <see cref="ProcessResult{T}"/> whose <c>Value</c> property is <c>true</c> if the file was deleted; otherwise, <c>false</c>.
    /// </returns>
    ProcessResult<bool> DeleteFile(string fullFilePath);

    /// <summary>
    /// Writes the specified lines of text to a file in the given directory.
    /// </summary>
    /// <param name="contentLines">The lines of text to write to the file.</param>
    /// <param name="fileName">The name of the file to write.</param>
    /// <param name="directoryPath">
    /// The directory in which to write the file. If <c>null</c>, the application directory is used.
    /// </param>
    /// <returns>
    /// A <see cref="ProcessResult{T}"/> whose <c>Value</c> property is <c>true</c> if the file was written successfully; 
    /// otherwise, <c>false</c>. If the operation fails, <see cref="ProcessResult{T}.Error"/> contains the exception.
    /// </returns>
    ProcessResult<bool> WriteFile(IEnumerable<string> contentLines, string fileName, string directoryPath = null);

    /// <summary>
    /// Retrieves the text content of an embedded resource from the specified assembly.
    /// </summary>
    /// <param name="assemblyEmbeddedIn">The assembly containing the embedded resource.</param>
    /// <param name="filePath">The full name of the embedded resource file.</param>
    /// <returns>
    /// A <see cref="ProcessResult{T}"/> whose <c>Value</c> property contains the text content of the embedded resource if successful.
    /// If the operation fails, <see cref="ProcessResult{T}.Error"/> contains the exception.
    /// </returns>
    ProcessResult<string> GetEmbeddedResourceText(Assembly assemblyEmbeddedIn, string filePath);
}
