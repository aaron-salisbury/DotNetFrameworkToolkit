using Cake.Common.Tools.MSBuild;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using System.IO;

namespace Build.Tasks.Standard;

[TaskName("Compile Projects")]
[IsDependentOn(typeof(RestoreTask))]
[TaskDescription("Builds the .NET Framework 2.0 solution with modern Visual Studio MSBuild.")]
public sealed class CompileProjectsTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.MSBuild(Path.Combine(context.SourceDirectory, "DotNetFrameworkToolkit.sln"), new MSBuildSettings
        {
            Target = "Build",
            Configuration = context.Config.ToString(),
            Verbosity = Verbosity.Minimal
        });
    }
}
