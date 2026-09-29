using OpenTK.Graphics.OpenGL4;
using SkiaSharp;

namespace GuiShark.OpenGL;

internal sealed class GpuTexture : IDisposable
{
    public int Handle { get; }
    public int Width { get; }
    public int Height { get; }

    public GpuTexture(SKBitmap bitmap)
    {
        Width = bitmap.Width;
        Height = bitmap.Height;
        Handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, Width, Height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, bitmap.GetPixels());
    }

    public void Dispose() => GL.DeleteTexture(Handle);
}
