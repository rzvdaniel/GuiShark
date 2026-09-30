using OpenTK.Graphics.OpenGL4;

namespace GuiShark.TextDemo;

/// <summary>Host-owned background and one-to-one framebuffer composition, independent of the UI SDK.</summary>
internal sealed class ScreenPainter : IDisposable
{
    private readonly int vao = GL.GenVertexArray();
    private readonly LabShader shader = new("""
        #version 330 core
        uniform vec2 viewport;
        uniform vec4 rect;
        out vec2 uv;
        const vec2 corners[6] = vec2[](vec2(0,0),vec2(1,0),vec2(1,1),vec2(0,0),vec2(1,1),vec2(0,1));
        void main() {
            uv = corners[gl_VertexID];
            vec2 p = rect.xy + uv * rect.zw;
            gl_Position = vec4(p.x/viewport.x*2.0-1.0, 1.0-p.y/viewport.y*2.0, 0, 1);
        }
        """, """
        #version 330 core
        in vec2 uv;
        out vec4 outputColor;
        uniform sampler2D image;
        uniform vec4 source;
        uniform int mode;
        uniform float time;
        void main() {
            if (mode == 3) {
                vec2 p = source.xy + uv * source.zw;
                outputColor = texture(image, vec2(p.x, 1.0-p.y));
            } else if (mode == 1) {
                outputColor = vec4(.93,.94,.92,1);
            } else if (mode == 2) {
                vec2 p = uv;
                vec3 color = mix(vec3(.055,.17,.20), vec3(.16,.30,.25), p.y);
                for (int i=0; i<4; i++) {
                    float n = float(i);
                    float hill = .24 + n*.18 + sin(p.x*6.0 + n*1.8 + time*.12)*.10;
                    color = mix(color, vec3(.07+n*.027,.18+n*.04,.12+n*.014), smoothstep(hill-.004,hill+.004,p.y));
                }
                outputColor = vec4(color,1);
            } else {
                outputColor = vec4(.043,.068,.10,1);
            }
        }
        """);

    private void Begin(int width, int height, UiRect rect, int mode)
    {
        GL.Viewport(0, 0, width, height);
        GL.Disable(EnableCap.ScissorTest); GL.Disable(EnableCap.DepthTest); GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.FramebufferSrgb);
        GL.UseProgram(shader.Handle); GL.BindVertexArray(vao);
        GL.Uniform2(shader["viewport"], (float)width, (float)height);
        GL.Uniform4(shader["rect"], rect.X, rect.Y, rect.Width, rect.Height);
        GL.Uniform1(shader["mode"], mode);
    }
    public void Background(int width, int height, int mode, float time)
    {
        Begin(width, height, new(0, 0, width, height), mode);
        GL.Uniform1(shader["time"], time);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
    public void Image(int width, int height, int texture, UiRect destination, UiRect source)
    {
        Begin(width, height, destination, 3);
        GL.ActiveTexture(TextureUnit.Texture0); GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.Uniform1(shader["image"], 0);
        GL.Uniform4(shader["source"], source.X, source.Y, source.Width, source.Height);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
    public void Dispose() { shader.Dispose(); GL.DeleteVertexArray(vao); }
}
