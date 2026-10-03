using System.Runtime.InteropServices;
using GuiShark.AppProtocol;
using SkiaSharp;

namespace GuiShark.ProcessHosting;

internal static class AppFrameEncoder
{
    public static AppMessage Encode(int width, int height, byte[] pixels, string? value, long sequence)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        for (var row = 0; row < height; row++)
            Marshal.Copy(pixels, (height - row - 1) * width * 4, bitmap.GetPixels() + row * bitmap.RowBytes, width * 4);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 0);
        return new AppMessage("frame", Value: value, Pixels: Convert.ToBase64String(encoded.ToArray()),
            PixelWidth: width, PixelHeight: height, Sequence: sequence);
    }
}
