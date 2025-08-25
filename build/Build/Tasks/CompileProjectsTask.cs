using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.MSBuild;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Build.Tasks;

[TaskName("Compile Projects")]
[IsDependentOn(typeof(LintingTask))]
[IsDependentOn(typeof(ProcessImagesTask))]
[TaskDescription("Compiles all projects in the src directory.")]
public sealed class CompileProjectsTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        string srcDir = System.IO.Path.Combine(context.AbsolutePathToRepo, "src");
        FilePathCollection csprojFiles = context.GetFiles(System.IO.Path.Combine(srcDir, "**/*.csproj"));

        foreach (FilePath csproj in csprojFiles)
        {
            if (BuildContext.IsSdkStyleProject(csproj.FullPath))
            {
                context.DotNetBuild(csproj.FullPath, new DotNetBuildSettings
                {
                    Configuration = context.Config.ToString(),
                    NoRestore = true
                });
            }
            else
            {
                context.MSBuild(csproj.FullPath, new MSBuildSettings()
                    .WithProperty("OutputPath", "bin\\net20")
                    .WithTarget("Build")
                    .SetConfiguration(context.Config.ToString())
                    .SetVerbosity(Verbosity.Minimal)
                );

                // Must multi-target the NuGet package so that only the .NET 2.0 DLL contains the ExtensionAttribute workaround.
                RunMultiTargetBuild(csproj, context);
            }
        }
    }

    private static void RunMultiTargetBuild(FilePath csproj, BuildContext context)
    {
        // Backup the original csproj
        string csprojPath = csproj.FullPath;
        string csprojBackupPath = csprojPath + ".bak";
        System.IO.File.Copy(csprojPath, csprojBackupPath, overwrite: true);

        try
        {
            // Load and modify the csproj
            XDocument doc = XDocument.Load(csprojPath);
            XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            // Update TargetFrameworkVersion to v3.5
            foreach (var tfv in doc.Descendants(ns + "TargetFrameworkVersion"))
            {
                tfv.Value = "v3.5";
            }

            // Remove NET20 from all DefineConstants
            foreach (XElement dc in doc.Descendants(ns + "DefineConstants"))
            {
                IEnumerable<string> constants = dc.Value.Split(';')
                    .Where(c => !string.Equals(c.Trim(), "NET20", StringComparison.OrdinalIgnoreCase));

                dc.Value = string.Join(";", constants);
            }

            // Save the modified csproj
            doc.Save(csprojPath);

            // Build for .NET 3.5
            context.MSBuild(csproj.FullPath, new MSBuildSettings()
                .WithProperty("OutputPath", "bin\\net35")
                .WithTarget("Build")
                .SetConfiguration(context.Config.ToString())
                .SetVerbosity(Verbosity.Minimal)
            );
        }
        finally
        {
            // Restore the original csproj
            System.IO.File.Copy(csprojBackupPath, csprojPath, overwrite: true);
            System.IO.File.Delete(csprojBackupPath);
        }
    }
}
