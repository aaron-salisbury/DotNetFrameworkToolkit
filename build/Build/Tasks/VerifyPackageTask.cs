using Build.Verification;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Build.Tasks;

[TaskName("Verify Package")]
[IsDependentOn(typeof(PackageTask))]
[TaskDescription("Validates package payload, dependencies, versions, Release binaries and portable symbol identity.")]
public sealed class VerifyPackageTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        ReleaseVersion version = ReleaseVersion.Load(context.AbsolutePathToRepo);
        string directory = Path.Combine(context.AbsolutePathToRepo, "artifacts", "packages", "Release");
        string basename = PackageVerifier.PACKAGE_ID + "." + version.PackageVersion;
        string package = Path.Combine(directory, basename + ".nupkg");
        string symbols = Path.Combine(directory, basename + ".snupkg");
        if (!File.Exists(package) || !File.Exists(symbols) || Directory.GetFiles(directory).Length != 2)
        {
            throw new InvalidDataException("Exactly the expected package and symbol package must be produced by the clean Release build.");
        }
        PackageVerifier.Verify(context.AbsolutePathToRepo, package, symbols);
        PackageVerifier.VerifyRejectionCases(context.AbsolutePathToRepo, package, symbols, Path.Combine(context.AbsolutePathToRepo, "artifacts", "package-verification"));
        File.WriteAllText(Path.Combine(directory, "validation.json"), JsonSerializer.Serialize(new
        {
            PackageVersion = version.PackageVersion,
            AssemblyVersion = version.AssemblyVersion.ToString(),
            FileVersion = version.FileVersion.ToString(),
            SourceCommit = PackageVerifier.GetSourceCommit(context.AbsolutePathToRepo),
            Configuration = "Release",
            Framework = "net20",
            RuntimeMetadata = "v2.0.50727",
            PackageSha256 = PackageVerifier.HashFile(package),
            SymbolsSha256 = PackageVerifier.HashFile(symbols),
            CorruptPackageFixturesRejected = 7,
            ImagesAndOverwriteVerified = true
        }, new JsonSerializerOptions { WriteIndented = true }));
        context.Log.Information("Release package and portable symbols verified; all seven corrupt package fixtures rejected.");
    }
}
