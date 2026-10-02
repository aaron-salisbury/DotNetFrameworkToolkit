using Build.Verification;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using SkiaSharp;
using System.IO;
using System.Threading.Tasks;
using static Build.BuildContext;

namespace Build.Tasks;

[TaskName("Verify Images")]
[IsDependentOn(typeof(ProcessImagesTask))]
[TaskDescription("Decodes generated PNG/ICO assets and verifies overwrite bounds and file disposal.")]
public sealed class VerifyImagesTask : AsyncFrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        return context.Config == BuildConfigurations.Release;
    }

    public override async Task RunAsync(BuildContext context)
    {
        string content = Path.Combine(context.AbsolutePathToRepo, "content");
        string source = Path.Combine(content, "logo", BuildContext.LOGO_SVG_FILENAME);
        using (SKBitmap logo = SKBitmap.Decode(Path.Combine(content, "logo.png")))
        {
            if (logo == null)
            {
                throw new InvalidDataException("Generated logo cannot be decoded.");
            }
            ImageVerifier.VerifyPng(await File.ReadAllBytesAsync(Path.Combine(content, "logo.png")), logo.Width, logo.Height);
        }
        foreach ((string name, int size) in new[] { ("icon-175.png", 175), ("extension-icon.png", 90), ("package-icon.png", 128) })
        {
            ImageVerifier.VerifyPng(await File.ReadAllBytesAsync(Path.Combine(content, name)), size, size);
        }
        ImageVerifier.VerifyIco(await File.ReadAllBytesAsync(Path.Combine(content, "favicon.ico")), 32);
        ImageVerifier.VerifyIco(await File.ReadAllBytesAsync(Path.Combine(content, "extension-icon.ico")), 64);
        await ImageVerifier.VerifyOverwriteAsync(source, Path.Combine(context.AbsolutePathToRepo, "artifacts", "image-verification"));
        context.Log.Information("PNG/ICO validation passed, including larger-to-smaller overwrite and exclusive file reopening.");
    }
}
