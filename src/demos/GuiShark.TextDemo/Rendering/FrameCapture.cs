using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using SkiaSharp;

namespace GuiShark.TextDemo;

internal static class FrameCapture
{
    public static void Save(int width, int height, string? outputPath)
    {
        var pixels = new byte[checked(width * height * 4)];
        GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        for (var y = 0; y < height; y++)
            Marshal.Copy(pixels, (height - y - 1) * width * 4, bitmap.GetPixels() + y * bitmap.RowBytes, width * 4);
        var path = outputPath ?? Path.GetFullPath(Path.Combine("artifacts", $"text-lab-{DateTime.Now:yyyyMMdd-HHmmss}.png"));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var output = File.Create(path);
        encoded.SaveTo(output); Console.WriteLine($"Screenshot: {path}");
    }
}
