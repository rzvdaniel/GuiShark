using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using SkiaSharp;

namespace GuiShark.Balloon;

internal static class FrameCapture
{
    public static void Save(int width, int height, string? outputPath = null)
    {
        if (width <= 0 || height <= 0) return;
        var pixels = new byte[width * height * 4];
        GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        for (var y = 0; y < height; y++)
            Marshal.Copy(pixels, (height - y - 1) * width * 4, bitmap.GetPixels() + y * bitmap.RowBytes, width * 4);
        Directory.CreateDirectory("artifacts");
        var path = outputPath ?? Path.GetFullPath(Path.Combine("artifacts", $"lantern-valley-{DateTime.Now:yyyyMMdd-HHmmss}.png"));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var output = File.Create(path);
        encoded.SaveTo(output);
        Console.WriteLine($"Screenshot: {path}");
    }
}
