using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Build.Verification;

internal static class ImageVerifier
{
    public static void VerifyPng(byte[] bytes, int width, int height)
    {
        byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (bytes.Length < signature.Length || !bytes.AsSpan(0, signature.Length).SequenceEqual(signature))
        {
            throw new InvalidDataException("Invalid PNG signature.");
        }
        using (SKBitmap bitmap = SKBitmap.Decode(bytes) ?? throw new InvalidDataException("PNG cannot be decoded."))
        {
            if (bitmap.Width != width || bitmap.Height != height)
            {
                throw new InvalidDataException("PNG dimensions do not match the requested image size.");
            }
            bool hasVisiblePixel = false;
            foreach (SKColor pixel in bitmap.Pixels)
            {
                hasVisiblePixel |= pixel.Alpha != 0;
            }
            if (!hasVisiblePixel)
            {
                throw new InvalidDataException("PNG is entirely transparent.");
            }
        }
    }

    public static void VerifyIco(byte[] bytes, int size)
    {
        using (MemoryStream stream = new(bytes, false))
        {
            using (BinaryReader reader = new(stream))
            {
                if (bytes.Length < 22 || reader.ReadUInt16() != 0 || reader.ReadUInt16() != 1 || reader.ReadUInt16() != 1)
                {
                    throw new InvalidDataException("ICO must have exactly one image.");
                }
                byte expected = (byte)(size == 256 ? 0 : size);
                if (reader.ReadByte() != expected || reader.ReadByte() != expected || reader.ReadByte() != 0 || reader.ReadByte() != 0 || reader.ReadUInt16() != 0 || reader.ReadUInt16() != 32)
                {
                    throw new InvalidDataException("Unexpected ICO image directory.");
                }
                uint length = reader.ReadUInt32();
                uint offset = reader.ReadUInt32();
                if (offset != 22 || length != bytes.Length - offset)
                {
                    throw new InvalidDataException("ICO payload bounds are invalid or contain trailing bytes.");
                }
                VerifyPng(reader.ReadBytes(checked((int)length)), size, size);
            }
        }
    }

    public static async Task VerifyOverwriteAsync(string source, string workingDirectory)
    {
        Directory.CreateDirectory(workingDirectory);
        string target = Path.Combine(workingDirectory, "overwrite.ico");
        try
        {
            await ImageRenderer.RenderIcoAsync(source, target, 256);
            long largeLength = new FileInfo(target).Length;
            VerifyIco(await File.ReadAllBytesAsync(target), 256);
            await ImageRenderer.RenderIcoAsync(source, target, 16);
            byte[] small = await File.ReadAllBytesAsync(target);
            if (small.Length >= largeLength)
            {
                throw new InvalidDataException("Overwrite fixture did not exercise a larger-to-smaller ICO.");
            }
            VerifyIco(small, 16);
            // A stale-tail ICO must fail even if its embedded PNG remains decodable.
            byte[] contaminated = new byte[small.Length + 100];
            small.CopyTo(contaminated, 0);
            ExpectFailure(() => VerifyIco(contaminated, 16));
            ExpectFailure(() => VerifyIco(small, 32));
            ExpectFailure(() => VerifyPng(new byte[8], 16, 16));
            string png = Path.Combine(workingDirectory, "overwrite.png");
            await File.WriteAllBytesAsync(png, new byte[largeLength + 100]);
            await ImageRenderer.RenderPngAsync(source, png, 16, 16);
            VerifyPng(await File.ReadAllBytesAsync(png), 16, 16);
            AssertExclusiveAccess(target);
            AssertExclusiveAccess(png);
            AssertExclusiveAccess(source);
        }
        finally
        {
            Directory.Delete(workingDirectory, true);
        }
    }

    private static void AssertExclusiveAccess(string path)
    {
        using (FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            if (stream.Length == 0)
            {
                throw new InvalidDataException("Image/source is empty.");
            }
        }
    }

    private static void ExpectFailure(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidDataException)
        {
            return;
        }
        throw new InvalidDataException("Image validator accepted an invalid fixture.");
    }
}
