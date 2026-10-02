using Build.Verification;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using System;

namespace Build.Tasks;

[TaskName("Validate Versions")]
[TaskDescription("Validates the central version values, optional release tag, and version policy regression cases.")]
public sealed class ValidateVersionTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        ReleaseVersion version = ReleaseVersion.Load(context.AbsolutePathToRepo);
        version.ValidateTag(context.Arguments.GetArgument("release-tag"), Environment.GetEnvironmentVariable("GITHUB_REF_TYPE"), Environment.GetEnvironmentVariable("GITHUB_REF_NAME"));
        ReleaseVersion.VerifyRules();
        context.Log.Information("Version policy passed: package {0}, assembly {1}, file {2}.", version.PackageVersion, version.AssemblyVersion, version.FileVersion);
    }
}
