using Build.Tasks.Standard;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Pack;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using static Build.BuildContext;

namespace Build.Tasks;

[TaskName("Package")]
[IsDependentOn(typeof(PublishTask))]
[TaskDescription("Generates the NuGet packages using previously processed images and project properties. Legacy projects should have a nuspec file, named after and next to the .csproj, to use as a template.")]
public sealed class PackageTask : AsyncFrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        return context.Config == BuildConfigurations.Release;
    }

    public override async Task RunAsync(BuildContext context)
    {
        foreach (ReleaseProject project in context.ReleaseProjects)
        {
            await PackageProjectAsync(context, project);
        }
    }

    private static async Task PackageProjectAsync(BuildContext context, ReleaseProject project)
    {
        string nuGetOutputPath = System.IO.Path.Combine(project.OutputDirectoryPathAbsolute, "NuGet");

        if (project.IsSdkStyleProject)
        {
            context.DotNetPack(project.CsprojFilePathAbsolute, new DotNetPackSettings
            {
                OutputDirectory = nuGetOutputPath,
                Configuration = context.Config.ToString(),
                NoRestore = true,
                NoBuild = true,

            });
        }
        else
        {
            // For legacy projects, use NuGet.
            string toolsDirectory = System.IO.Path.Combine(context.AbsolutePathToRepo, "tools");
            string nugetExePath = await VerifyNuGetToolAsync(context, toolsDirectory);
            string nuspecPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(project.CsprojFilePathAbsolute)!, $"{project.Name}.nuspec");

            if (!System.IO.File.Exists(nuspecPath))
            {
                throw new InvalidOperationException($"Required nuspec file not found: {nuspecPath}");
            }

            context.NuGetPack(nuspecPath, new NuGetPackSettings
            {
                ToolPath = nugetExePath,
                OutputDirectory = nuGetOutputPath,
                Properties = new Dictionary<string, string> { { "Configuration", context.Config.ToString() } },
                NoPackageAnalysis = true,
                IncludeReferencedProjects = true,
                Symbols = false
            });
        }
    }

    private static async Task<string> VerifyNuGetToolAsync(BuildContext context, string toolsDirectory)
    {
        string nugetExePath = System.IO.Path.Combine(toolsDirectory, "nuget.exe");

        if (!System.IO.File.Exists(nugetExePath))
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            context.Log.Information("NuGet tool not found. Downloading latest version...");

            System.IO.Directory.CreateDirectory(toolsDirectory);
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe");
            response.EnsureSuccessStatusCode();
            using var fs = new System.IO.FileStream(nugetExePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
            await response.Content.CopyToAsync(fs);

            stopwatch.Stop();
            double completionTime = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
            context.Log.Information($"NuGet tool download complete ({completionTime}s)");
        }

        return nugetExePath;
    }
}
