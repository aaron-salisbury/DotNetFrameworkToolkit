using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.MSBuild;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("Compile Projects")]
[IsDependentOn(typeof(LintingTask))]
[IsDependentOn(typeof(ProcessImagesTask))]
[TaskDescription("Compiles all projects in the src directory, excluding the Build project.")]
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
                context.MSBuild(csproj.FullPath, new MSBuildSettings
                {
                    Target = "Build",
                    Configuration = context.Config.ToString(),
                    Verbosity = Verbosity.Minimal
                });
            }
        }
    }
}
