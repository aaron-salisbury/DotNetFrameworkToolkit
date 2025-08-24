using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.MSBuild;
using Cake.Frosting;
using static Build.BuildContext;

namespace Build.Tasks;

[TaskName("Publish")]
[IsDependentOn(typeof(CompileProjectsTask))]
[TaskDescription("Generates the NuGet packages. " +
    "SDK-style projects should have package attributes set in the .csproj. " +
    "Legacy projects should have a nuspec file, named after and next to the .csproj, to use as a template.")]
public sealed class PublishTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        return context.Config == BuildConfigurations.Release;
    }

    public override void Run(BuildContext context)
    {
        foreach (ReleaseProject project in context.ReleaseProjects)
        {
            if (project.IsSdkStyleProject)
            {
                context.DotNetPublish(project.FilePathAbsolute, new DotNetPublishSettings
                {
                    Configuration = context.Config.ToString()
                });
            }
            else
            {
                context.MSBuild(project.FilePathAbsolute, new MSBuildSettings
                {
                    Target = "Publish",
                    Configuration = context.Config.ToString()
                });
            }
        }
    }
}
