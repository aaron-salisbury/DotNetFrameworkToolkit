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
using System.IO.Compression;
using System.Linq;
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
                NoBuild = true
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
                //Symbols = false  // nuget.exe pack does not support generating portable PDBs for legacy (non-SDK style) projects
                Symbols = true,
                SymbolPackageFormat = "snupkg"
            });

            // Manually create the .snupkg symbol package
            //await CreateSymbolPackageAsync(context, project, nuGetOutputPath);
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

    private static async Task CreateSymbolPackageAsync(BuildContext context, ReleaseProject project, string nuGetOutputPath)
    {
        // Find the generated .nupkg file
        var nupkgFiles = System.IO.Directory.GetFiles(nuGetOutputPath, "*.nupkg")
            .Where(f => !f.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => System.IO.File.GetLastWriteTime(f))
            .ToArray();

        if (nupkgFiles.Length == 0)
        {
            context.Log.Warning($"No .nupkg file found in {nuGetOutputPath}. Skipping symbol package creation.");
            return;
        }

        string nupkgPath = nupkgFiles[0];
        string snupkgPath = System.IO.Path.ChangeExtension(nupkgPath, ".snupkg");

        context.Log.Information($"Creating symbol package: {System.IO.Path.GetFileName(snupkgPath)}");

        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        System.IO.Directory.CreateDirectory(tempDir);

        try
        {
            string pdbSourceDir = project.OutputDirectoryPathAbsolute;

            if (!System.IO.Directory.Exists(pdbSourceDir))
            {
                context.Log.Warning($"PDB source directory not found: {pdbSourceDir}");
                return;
            }

            var pdbFiles = System.IO.Directory.GetFiles(pdbSourceDir, "*.pdb");

            if (pdbFiles.Length == 0)
            {
                context.Log.Warning($"No PDB files found in {pdbSourceDir}");
                return;
            }

            string symbolLibDir = System.IO.Path.Combine(tempDir, "lib", "net20");
            System.IO.Directory.CreateDirectory(symbolLibDir);

            foreach (string pdbFile in pdbFiles)
            {
                string destPath = System.IO.Path.Combine(symbolLibDir, System.IO.Path.GetFileName(pdbFile));
                System.IO.File.Copy(pdbFile, destPath, overwrite: true);
                context.Log.Information($"  Added: lib/net20/{System.IO.Path.GetFileName(pdbFile)}");
            }

            if (System.IO.File.Exists(snupkgPath))
            {
                System.IO.File.Delete(snupkgPath);
            }

            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, snupkgPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);

            context.Log.Information($"Successfully created symbol package: {System.IO.Path.GetFileName(snupkgPath)}");
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
            {
                System.IO.Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
