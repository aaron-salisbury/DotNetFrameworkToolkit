using Build.Tasks.Standard;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using SkiaSharp;
using Svg.Skia;
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
        await RenderSvgToPngAsync(sourceSVGPath, pngPath);

        // Create deployment icons directly from the source SVG at their target sizes.
        context.Log.Information($"Creating icons suitable for various deployments...");
        await Task.WhenAll(
            RenderSvgToIcoAsync(sourceSVGPath, Path.Combine(contentDir, "favicon.ico"), 32),
            RenderSvgToIcoAsync(sourceSVGPath, Path.Combine(contentDir, "extension-icon.ico"), 64),
            RenderSvgToPngAsync(sourceSVGPath, Path.Combine(contentDir, "icon-175.png"), 175, 175),
            RenderSvgToPngAsync(sourceSVGPath, Path.Combine(contentDir, "extension-icon.png"), 90, 90),
            RenderSvgToPngAsync(sourceSVGPath, Path.Combine(contentDir, "package-icon.png"), 128, 128)
        );

        stopwatch.Stop();
        double completionTime = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
        context.Log.Information($"Processing of project images complete ({completionTime}s)");
    }

    private static async Task RenderSvgToPngAsync(string sourceSvgPath, string targetPngPath, int? width = null, int? height = null)
    {
        using SKBitmap bitmap = RenderSvg(sourceSvgPath, width, height);
        byte[] pngData = EncodePng(bitmap);
        await File.WriteAllBytesAsync(targetPngPath, pngData);
    }

    private static async Task RenderSvgToIcoAsync(string sourceSvgPath, string targetIcoPath, int iconSize)
    {
        // ref: https://www.meziantou.net/creating-ico-files-from-multiple-images-in-dotnet.htm

        const short NUM_IMAGES = 1;

        using SKBitmap bitmap = RenderSvg(sourceSvgPath, iconSize, iconSize);
        byte[] pngData = EncodePng(bitmap);

        await using FileStream output = File.OpenWrite(targetIcoPath);
        await using BinaryWriter iconWriter = new(output);

        iconWriter.Write((byte)0); // reserved
        iconWriter.Write((byte)0);
        iconWriter.Write((short)1); // image type: icon
        iconWriter.Write(NUM_IMAGES); // number of images

        long offset = 6 + (16 * NUM_IMAGES); // ico header (6 bytes) + image directory (16 bytes per image)

        iconWriter.Write((byte)(iconSize >= 256 ? 0 : iconSize));
        iconWriter.Write((byte)(iconSize >= 256 ? 0 : iconSize));
        iconWriter.Write((byte)0); // number of colors
        iconWriter.Write((byte)0); // reserved
        iconWriter.Write((short)0); // color planes
        iconWriter.Write((short)32); // bits per pixel
        iconWriter.Write((uint)pngData.Length); // size of image data
        iconWriter.Write((uint)offset); // offset of image data
        iconWriter.Write(pngData);
    }

    private static SKBitmap RenderSvg(string sourceSvgPath, int? targetWidth = null, int? targetHeight = null)
    {
        SKSvg svg = new();
        svg.Load(sourceSvgPath);

        if (svg.Picture == null)
        {
            throw new InvalidOperationException("Failed to load SVG picture.");
        }

        SKRect bounds = svg.Picture.CullRect;
        int width = targetWidth ?? (int)Math.Ceiling(bounds.Width);
        int height = targetHeight ?? (int)Math.Ceiling(bounds.Height);

        SKBitmap bitmap = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);

        float scaleX = width / bounds.Width;
        float scaleY = height / bounds.Height;
        canvas.Scale(scaleX, scaleY);
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.DrawPicture(svg.Picture);

        return bitmap;
    }

    private static byte[] EncodePng(SKBitmap bitmap)
    {
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
