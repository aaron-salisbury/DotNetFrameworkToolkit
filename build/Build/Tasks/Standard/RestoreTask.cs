using Cake.Common;
using Cake.Frosting;
using System;
using System.IO;

namespace Build.Tasks.Standard;

[TaskName("Restore")]
[IsDependentOn(typeof(CleanTask))]
[TaskDescription("Restores the legacy solution's packages.config dependencies, including .NET 2.0 reference assemblies.")]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        string solution = Path.Combine(context.SourceDirectory, "DotNetFrameworkToolkit.sln");
        string packages = Path.Combine(context.SourceDirectory, "packages");
        int exit = context.StartProcess("nuget", $"restore \"{solution}\" -PackagesDirectory \"{packages}\" -NonInteractive");
        if (exit != 0)
        {
            throw new InvalidOperationException("NuGet restore failed with exit code " + exit);
        }
    }
}
