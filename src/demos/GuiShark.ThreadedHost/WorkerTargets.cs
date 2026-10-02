using OpenTK.Graphics.OpenGL4;

namespace GuiShark.ThreadedHost;

internal sealed class WorkerTargets : IDisposable
{
    private readonly SharedFrames shared;
    private readonly int[] framebuffers = new int[3];
    private readonly int[] textures = new int[3];
    private bool disposed;

    public WorkerTargets(SharedFrames shared, AppSize size)
    {
        this.shared = shared;
        try
        {
            for (var slot = 0; slot < textures.Length; slot++)
            {
                textures[slot] = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, textures[slot]);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, size.PixelWidth, size.PixelHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                framebuffers[slot] = GL.GenFramebuffer();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffers[slot]);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textures[slot], 0);
                if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                    throw new InvalidOperationException("App framebuffer is incomplete.");
                shared.SetTexture(slot, textures[slot], size);
            }
        }
        catch
        {
            Dispose();
            throw;
        }
        finally
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }

    public void Bind(int slot) => GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffers[slot]);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        shared.Retire(() =>
        {
            for (var slot = 0; slot < textures.Length; slot++)
            {
                if (framebuffers[slot] != 0) GL.DeleteFramebuffer(framebuffers[slot]);
                if (textures[slot] != 0) GL.DeleteTexture(textures[slot]);
            }
        });
    }
}
