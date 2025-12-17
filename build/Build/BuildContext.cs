using Cake.Common.IO;
using Cake.Common.IO.Paths;
using Cake.Common.Xml;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace Build;

public sealed class BuildContext : FrostingContext
{
    internal const string REPO_NAME = "DotNetFrameworkToolkit";
    internal const string LOGO_SVG_FILENAME = "logo.svg";

    private static readonly string[] RELEASE_PROJECT_NAMES = [ "DotNetFrameworkToolkit" ];

    public enum BuildConfigurations
    {
        Debug,
        Release
    }

    public BuildConfigurations Config { get; }
    public JsonSerializerOptions SerializerOptions { get; }
    public string AbsolutePathToRepo { get; }
    public ConvertableDirectoryPath SourceDirectory { get; }
    public List<ReleaseProject> ReleaseProjects { get; }

    public BuildContext(ICakeContext context) : base(context)
    {
        string configArgument = context.Arguments.GetArgument("Configuration") ?? string.Empty;
        Config = configArgument.ToLower() switch
        {
            "release" => BuildConfigurations.Release,
            _ => BuildConfigurations.Debug,
        };

        SerializerOptions = new() { PropertyNameCaseInsensitive = true };
        AbsolutePathToRepo = GetRepoAbsolutePath(REPO_NAME, this);
        SourceDirectory = AbsolutePathToRepo + context.Directory("src");
        ReleaseProjects = [.. RELEASE_PROJECT_NAMES.Select(name => CreateReleaseProject(this, name))];
    }

    private static string GetRepoAbsolutePath(string repoName, ICakeContext context)
    {
        // Start from the working directory.
        DirectoryPath dir = context.Environment.WorkingDirectory;

        // Traverse up until we find the directory named after the repository name.
        while (dir != null && !dir.GetDirectoryName().Equals(repoName, StringComparison.OrdinalIgnoreCase))
        {
            dir = dir.GetParent();
        }

        if (dir == null)
        {
            throw new InvalidOperationException($"Could not find repository root directory named '{repoName}' in parent chain.");
        }

        return dir.FullPath;
    }

    private static ReleaseProject CreateReleaseProject(BuildContext context, string projectName)
    {
        ConvertableDirectoryPath baseProjectDirectory = context.SourceDirectory + context.Directory(projectName);
        string csprojPath = baseProjectDirectory + context.File($"{projectName}.csproj");
        bool isSdkStyleProject = IsSdkStyleProject(csprojPath);
        string targetVersion = GetTargetFramework(csprojPath, isSdkStyleProject, context);

        return new ReleaseProject
        {
            Name = projectName,
            DirectoryPathAbsolute = baseProjectDirectory,
            CsprojFilePathAbsolute = csprojPath,
            OutputDirectoryPathAbsolute = baseProjectDirectory + context.Directory($"bin/{context.Config}/{targetVersion}"),
            IsSdkStyleProject = isSdkStyleProject
        };
    }

    private static bool IsSdkStyleProject(string csprojPath)
    {
        XDocument doc = XDocument.Load(csprojPath);
        XElement? projectElement = doc.Root;

        return projectElement?.Attribute("Sdk") != null;
    }

    private static string GetTargetFramework(string csprojPath, bool isSdkStyleProject, ICakeContext context)
    {
        if (isSdkStyleProject)
        {
            // Only supporting single TargetFramework for now.
            return context.XmlPeek(csprojPath, "/Project/PropertyGroup/TargetFramework");
        }

        XDocument doc = XDocument.Load(csprojPath);
        XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        string? targetFrameworkVersion = doc.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault()?.Value;

        if (string.IsNullOrEmpty(targetFrameworkVersion))
        {
            throw new InvalidOperationException($"Could not find TargetFrameworkVersion in {csprojPath}");
        }

        // Convert "v2.0" to "net20" format
        return targetFrameworkVersion.Replace("v", "net").Replace(".", string.Empty);
    }
}
