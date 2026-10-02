using Build.Tasks.Standard;
using Build.Verification;
using Cake.Common.Tools.NuGet.Pack;
using Cake.Common.Tools.NuGet;
using Cake.Core;
using Cake.Frosting;
using System.Collections.Generic;
using System.IO;
using System;
using static Build.BuildContext;

namespace Build.Tasks;

[TaskName("Package")]
[IsDependentOn(typeof(PublishTask))]
[IsDependentOn(typeof(VerifyImagesTask))]
[TaskDescription("Creates Release NuGet and portable-symbol packages with NuGet analysis enabled.")]
public sealed class PackageTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.Config != BuildConfigurations.Release)
        {
            throw new InvalidOperationException("Packaging requires --configuration=Release.");
        }

        ReleaseVersion version = ReleaseVersion.Load(context.AbsolutePathToRepo);
        version.ValidateTag(context.Arguments.GetArgument("release-tag"), Environment.GetEnvironmentVariable("GITHUB_REF_TYPE"), Environment.GetEnvironmentVariable("GITHUB_REF_NAME"));
        string output = Path.Combine(context.AbsolutePathToRepo, "artifacts", "packages", "Release");
        Directory.CreateDirectory(output);
        foreach (ReleaseProject project in context.ReleaseProjects)
        {
            string nuspec = Path.Combine(project.DirectoryPathAbsolute, project.Name + ".nuspec");
            context.NuGetPack(nuspec, new NuGetPackSettings
            {
                OutputDirectory = output,
                Properties = new Dictionary<string, string>
                {
                    { "Configuration", "Release" },
                    { "version", version.PackageVersion },
                    { "commit", PackageVerifier.GetSourceCommit(context.AbsolutePathToRepo) }
                },
                NoPackageAnalysis = false,
                Symbols = true,
                SymbolPackageFormat = "snupkg"
            });
        }
    }
}
