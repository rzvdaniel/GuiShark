using OpenTK.Graphics.OpenGL4;

namespace GuiShark.OpenGL;

internal sealed class QuadPainter : IDisposable
{
    private readonly ShaderProgram program = new(QuadShaders.Vertex, QuadShaders.Fragment);
    private readonly int vao = GL.GenVertexArray();
    private float scaleX, scaleY;
    private int framebufferHeight;

    public void Begin(float width, float height, int pixelWidth, int pixelHeight)
    {
        scaleX = pixelWidth / width;
        scaleY = pixelHeight / height;
        framebufferHeight = pixelHeight;
        GL.UseProgram(program.Handle);
        GL.BindVertexArray(vao);
        GL.Uniform2(program["viewport"], width, height);
        GL.Uniform1(program["image"], 0);
        GL.Viewport(0, 0, pixelWidth, pixelHeight);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        GL.ColorMask(true, true, true, true);
        foreach (var cap in new[] { EnableCap.DepthTest, EnableCap.StencilTest, EnableCap.CullFace,
            EnableCap.FramebufferSrgb, EnableCap.RasterizerDiscard, EnableCap.ColorLogicOp }) GL.Disable(cap);
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
    }

    public void Clip(UiRect clip)
    {
        var left = (int)Math.Floor(clip.X * scaleX);
        var top = (int)Math.Floor(clip.Y * scaleY);
        var right = (int)Math.Ceiling(clip.Right * scaleX);
        var bottom = (int)Math.Ceiling(clip.Bottom * scaleY);
        GL.Scissor(left, framebufferHeight - bottom, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    public void Solid(UiRect rect, UiColor color, float opacity, float radius = 0)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;
        SetRect(rect, radius, 0);
        SetColor("topColor", color, opacity);
        SetColor("bottomColor", color, opacity);
        SetColor("borderColor", UiColor.Transparent, opacity);
        GL.Uniform1(program["textured"], 0);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void Shape(UiRect rect, UiStyle style, float opacity)
    {
        if (style.BackgroundInset == default)
            PaintShape(rect, style, opacity, fill: true, border: true);
        else
        {
            PaintShape(rect.Inset(style.BackgroundInset), style, opacity, fill: true, border: false);
            PaintShape(rect, style, opacity, fill: false, border: true);
        }
    }

    private void PaintShape(UiRect rect, UiStyle style, float opacity, bool fill, bool border)
    {
        var top = fill ? style.Background : UiColor.Transparent;
        var bottom = fill ? style.GradientEnd : UiColor.Transparent;
        var borderWidth = border ? style.BorderWidth : 0;
        if (rect.Width <= 0 || rect.Height <= 0 || (top.A == 0 && bottom.A == 0 && borderWidth == 0)) return;
        SetRect(rect, style.Radius, borderWidth);
        SetColor("topColor", top, opacity);
        SetColor("bottomColor", bottom, opacity);
        SetColor("borderColor", style.BorderColor, opacity);
        GL.Uniform1(program["textured"], 0);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void Texture(UiRect rect, GpuTexture texture, UiColor color, float opacity, float radius = 0, UiRect? source = null,
        TextSampling sampling = TextSampling.Linear, float distanceRange = 0)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;
        SetRect(rect, radius, 0);
        SetColor("topColor", color, opacity);
        var uv = source ?? new UiRect(0, 0, 1, 1);
        GL.Uniform4(program["textureRect"], uv.X, uv.Y, uv.Width, uv.Height);
        GL.Uniform1(program["textured"], 1);
        GL.Uniform1(program["distanceRange"], distanceRange);
        GL.BindTexture(TextureTarget.Texture2D, texture.Handle);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)(sampling == TextSampling.Nearest ? TextureMinFilter.Nearest : TextureMinFilter.Linear));
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)(sampling == TextSampling.Nearest ? TextureMagFilter.Nearest : TextureMagFilter.Linear));
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    private void SetRect(UiRect rect, float radius, float border)
    {
        GL.Uniform4(program["rect"], rect.X, rect.Y, rect.Width, rect.Height);
        GL.Uniform1(program["radius"], radius);
        GL.Uniform1(program["borderWidth"], border);
    }

    private void SetColor(string name, UiColor color, float opacity) =>
        GL.Uniform4(program[name], color.R, color.G, color.B, color.A * opacity);

    public void Dispose() { program.Dispose(); GL.DeleteVertexArray(vao); }
}
