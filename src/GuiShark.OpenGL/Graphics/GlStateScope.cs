using OpenTK.Graphics.OpenGL4;

namespace GuiShark.OpenGL;

/// <summary>Snapshots only state touched by this renderer; never binds or clears the host framebuffer.</summary>
internal sealed class GlStateScope : IDisposable
{
    private readonly int program = GL.GetInteger(GetPName.CurrentProgram);
    private readonly int vao = GL.GetInteger(GetPName.VertexArrayBinding);
    private readonly int activeTexture = GL.GetInteger(GetPName.ActiveTexture);
    private readonly int unpackBuffer = GL.GetInteger(GetPName.PixelUnpackBufferBinding);
    private readonly int texture;
    private readonly int sampler;
    private readonly int[] viewport = new int[4];
    private readonly int[] scissor = new int[4];
    private readonly int[] polygon = new int[2];
    private readonly bool[] colorMask = new bool[4];
    private readonly Dictionary<EnableCap, bool> enabled;
    private readonly Dictionary<PixelStoreParameter, int> pixelStore;
    private readonly int sourceRgb = GL.GetInteger(GetPName.BlendSrcRgb);
    private readonly int destinationRgb = GL.GetInteger(GetPName.BlendDstRgb);
    private readonly int sourceAlpha = GL.GetInteger(GetPName.BlendSrcAlpha);
    private readonly int destinationAlpha = GL.GetInteger(GetPName.BlendDstAlpha);
    private readonly int equationRgb = GL.GetInteger(GetPName.BlendEquationRgb);
    private readonly int equationAlpha = GL.GetInteger(GetPName.BlendEquationAlpha);

    public GlStateScope()
    {
        GL.GetInteger(GetPName.Viewport, viewport);
        GL.GetInteger(GetPName.ScissorBox, scissor);
        GL.GetInteger(GetPName.PolygonMode, polygon);
        GL.GetBoolean(GetPName.ColorWritemask, colorMask);
        enabled = new[] { EnableCap.Blend, EnableCap.DepthTest, EnableCap.StencilTest, EnableCap.CullFace,
            EnableCap.ScissorTest, EnableCap.FramebufferSrgb, EnableCap.RasterizerDiscard, EnableCap.ColorLogicOp }
            .ToDictionary(cap => cap, GL.IsEnabled);
        pixelStore = new[] { PixelStoreParameter.UnpackAlignment, PixelStoreParameter.UnpackRowLength,
            PixelStoreParameter.UnpackSkipRows, PixelStoreParameter.UnpackSkipPixels }.ToDictionary(p => p, p => GL.GetInteger((GetPName)p));
        GL.ActiveTexture(TextureUnit.Texture0);
        texture = GL.GetInteger(GetPName.TextureBinding2D);
        sampler = GL.GetInteger(GetPName.SamplerBinding);
        GL.BindSampler(0, 0);
        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
        foreach (var parameter in pixelStore.Keys) GL.PixelStore(parameter, parameter == PixelStoreParameter.UnpackAlignment ? 4 : 0);
    }

    public void Dispose()
    {
        GL.UseProgram(program);
        GL.BindVertexArray(vao);
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.BindSampler(0, sampler);
        GL.ActiveTexture((TextureUnit)activeTexture);
        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, unpackBuffer);
        foreach (var (parameter, value) in pixelStore) GL.PixelStore(parameter, value);
        GL.Viewport(viewport[0], viewport[1], viewport[2], viewport[3]);
        GL.Scissor(scissor[0], scissor[1], scissor[2], scissor[3]);
        GL.PolygonMode(TriangleFace.FrontAndBack, (PolygonMode)polygon[0]);
        GL.ColorMask(colorMask[0], colorMask[1], colorMask[2], colorMask[3]);
        GL.BlendFuncSeparate((BlendingFactorSrc)sourceRgb, (BlendingFactorDest)destinationRgb,
            (BlendingFactorSrc)sourceAlpha, (BlendingFactorDest)destinationAlpha);
        GL.BlendEquationSeparate((BlendEquationMode)equationRgb, (BlendEquationMode)equationAlpha);
        foreach (var (cap, value) in enabled) { if (value) GL.Enable(cap); else GL.Disable(cap); }
    }
}
