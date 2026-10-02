using SkiaSharp;
using Svg.Skia;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Build.Verification;

internal static class ImageRenderer
{
    public static async Task RenderPngAsync(string source, string target, int? width = null, int? height = null)
    {
        using (SKBitmap bitmap = RenderSvg(source, width, height))
        {
            await File.WriteAllBytesAsync(target, EncodePng(bitmap));
        }
    }

    public static async Task RenderIcoAsync(string source, string target, int size)
    {
        if (size < 1 || size > 256)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }
        using (SKBitmap bitmap = RenderSvg(source, size, size))
        {
            byte[] png = EncodePng(bitmap);
            // One PNG-backed ICO image: 6-byte header, 16-byte directory, then payload.
            // https://www.meziantou.net/creating-ico-files-from-multiple-images-in-dotnet.htm
            using (FileStream output = new(target, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (BinaryWriter writer = new(output))
                {
                    writer.Write((ushort)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)1);
                    writer.Write((byte)(size == 256 ? 0 : size));
                    writer.Write((byte)(size == 256 ? 0 : size));
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)32);
                    writer.Write((uint)png.Length);
                    writer.Write((uint)22);
                    writer.Write(png);
                    await output.FlushAsync();
                }
            }
        }
    }

    private static SKBitmap RenderSvg(string source, int? targetWidth, int? targetHeight)
    {
        using (SKSvg svg = new())
        {
            svg.Load(source);
            SKPicture picture = svg.Picture ?? throw new InvalidDataException("Failed to load SVG picture.");
            SKRect bounds = picture.CullRect;
            int width = targetWidth ?? checked((int)Math.Ceiling(bounds.Width));
            int height = targetHeight ?? checked((int)Math.Ceiling(bounds.Height));
            if (width < 1 || height < 1 || !float.IsFinite(bounds.Width) || !float.IsFinite(bounds.Height) || bounds.Width <= 0 || bounds.Height <= 0)
            {
                throw new InvalidDataException("SVG and target dimensions must be positive and finite.");
            }
            using (SKColorSpace colorSpace = SKColorSpace.CreateSrgb())
            {
                SKBitmap bitmap = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul, colorSpace);
                try
                {
                    using (SKCanvas canvas = new(bitmap))
                    {
                        canvas.Clear(SKColors.Transparent);
                        canvas.Scale(width / bounds.Width, height / bounds.Height);
                        canvas.Translate(-bounds.Left, -bounds.Top);
                        canvas.DrawPicture(picture);
                    }
                    return bitmap;
                }
                catch
                {
                    bitmap.Dispose();
                    throw;
                }
            }
        }
    }

    private static byte[] EncodePng(SKBitmap bitmap)
    {
        using (SKImage image = SKImage.FromBitmap(bitmap))
        {
            using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100) ?? throw new InvalidDataException("PNG encoding failed."))
            {
                return data.ToArray();
            }
        }
    }
}
