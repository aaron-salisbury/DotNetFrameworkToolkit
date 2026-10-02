using Build.Tasks.Standard;
using Build.Verification;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static Build.BuildContext;

namespace Build.Tasks;

[TaskName("Process Images")]
[IsDependentOn(typeof(RestoreTask))]
[TaskDescription("Processes source logo image to be used in the read-me and as release icons.")]
public sealed class ProcessImagesTask : AsyncFrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        return context.Config == BuildConfigurations.Release;
    }

    public override async Task RunAsync(BuildContext context)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        string contentDir = Path.Combine(context.AbsolutePathToRepo, "content");

        // Convert source logo SVG to PNG and save to content folder. Used in the read-me markdown document.
        context.Log.Information($"Creating project logo image (PNG) from source SVG file...");
        string sourceSVGPath = Path.Combine(contentDir, "logo", BuildContext.LOGO_SVG_FILENAME);
        string pngPath = Path.Combine(contentDir, "logo.png");
        await ImageRenderer.RenderPngAsync(sourceSVGPath, pngPath);

        // Create deployment icons directly from the source SVG at their target sizes.
        context.Log.Information($"Creating icons suitable for various deployments...");
        await Task.WhenAll(
            ImageRenderer.RenderIcoAsync(sourceSVGPath, Path.Combine(contentDir, "favicon.ico"), 32),
            ImageRenderer.RenderIcoAsync(sourceSVGPath, Path.Combine(contentDir, "extension-icon.ico"), 64),
            ImageRenderer.RenderPngAsync(sourceSVGPath, Path.Combine(contentDir, "icon-175.png"), 175, 175),
            ImageRenderer.RenderPngAsync(sourceSVGPath, Path.Combine(contentDir, "extension-icon.png"), 90, 90),
            ImageRenderer.RenderPngAsync(sourceSVGPath, Path.Combine(contentDir, "package-icon.png"), 128, 128)
        );

        stopwatch.Stop();
        double completionTime = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
        context.Log.Information($"Processing of project images complete ({completionTime}s)");
    }

}
