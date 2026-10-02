using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using SkiaSharp;

namespace GuiShark.ThreadedHost;

internal static class FrameCapture
{
    public static void Save(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        for (var row = 0; row < height; row++)
            Marshal.Copy(pixels, (height - row - 1) * width * 4, bitmap.GetPixels() + row * bitmap.RowBytes, width * 4);
        Directory.CreateDirectory("artifacts");
        var path = Path.GetFullPath("artifacts/threaded-host.png");
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var output = File.Create(path);
        encoded.SaveTo(output);
        Console.WriteLine($"Screenshot saved: {path}");
    }
}
