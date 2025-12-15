using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.MSBuild;
using Cake.Frosting;
using static Build.BuildContext;

namespace Build.Tasks.Standard;

[TaskName("Publish")]
[IsDependentOn(typeof(CompileProjectsTask))]
[TaskDescription("Publishes projects using the Release configuration, applying publish settings defined in their .csproj files.")]
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
            PublishProject(context, project);
        }
    }

    private static void PublishProject(BuildContext context, ReleaseProject project)
    {
        if (project.IsSdkStyleProject)
        {
            context.DotNetPublish(project.CsprojFilePathAbsolute, new DotNetPublishSettings
            {
                Configuration = context.Config.ToString()
            });
        }
        else
        {
            context.MSBuild(project.CsprojFilePathAbsolute, new MSBuildSettings
            {
                Target = "Publish",
                Configuration = context.Config.ToString()
            });
        }
    }
}
