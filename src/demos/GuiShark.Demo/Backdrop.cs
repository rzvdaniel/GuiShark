using OpenTK.Graphics.OpenGL4;

namespace GuiShark.Demo;

/// <summary>An independent host scene, rendered before the embedded UI.</summary>
internal sealed class Backdrop : IDisposable
{
    private readonly int program;
    private readonly int vao;
    private readonly int timeLocation;
    private readonly int darkLocation;

    public Backdrop()
    {
        var vertex = Compile(ShaderType.VertexShader, """
            #version 330 core
            out vec2 uv;
            void main() {
                vec2 p = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);
                uv = p;
                gl_Position = vec4(p * 2.0 - 1.0, 0, 1);
            }
            """);
        var fragment = Compile(ShaderType.FragmentShader, """
            #version 330 core
            in vec2 uv;
            out vec4 color;
            uniform float time;
            uniform bool dark;
            void main() {
                vec3 base = mix(vec3(.055,.11,.17), vec3(.16,.24,.31), uv.y);
                float glow = exp(-4.0 * length(uv - vec2(.7 + .06*sin(time*.12), .65)));
                base += glow * vec3(.05,.13,.16);
                float wave = sin((uv.x + uv.y*.5)*20.0 + time*.15)*.04;
                float contour = abs(fract((uv.y + uv.x*.25 + wave)*14.0)-.5);
                base += (1.0-smoothstep(.006,.018,contour))*.018;
                color = vec4(base * (dark ? .65 : 1.0), 1);
            }
            """);
        program = GL.CreateProgram();
        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var linked);
        if (linked == 0) throw new InvalidOperationException(GL.GetProgramInfoLog(program));
        vao = GL.GenVertexArray();
        timeLocation = GL.GetUniformLocation(program, "time");
        darkLocation = GL.GetUniformLocation(program, "dark");
    }

    public void Render(int width, int height, float time, bool dark)
    {
        GL.Viewport(0, 0, width, height);
        GL.Disable(EnableCap.ScissorTest);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Blend);
        GL.ClearColor(.06f, .1f, .16f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.UseProgram(program);
        GL.BindVertexArray(vao);
        GL.Uniform1(timeLocation, time);
        GL.Uniform1(darkLocation, dark ? 1 : 0);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
    }

    private static int Compile(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var compiled);
        if (compiled == 0) throw new InvalidOperationException(GL.GetShaderInfoLog(shader));
        return shader;
    }

    public void Dispose() { GL.DeleteVertexArray(vao); GL.DeleteProgram(program); }
}
