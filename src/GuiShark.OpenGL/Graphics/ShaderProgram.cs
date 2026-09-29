using OpenTK.Graphics.OpenGL4;

namespace GuiShark.OpenGL;

internal sealed class ShaderProgram : IDisposable
{
    private readonly Dictionary<string, int> uniforms = new();
    public int Handle { get; }

    public ShaderProgram(string vertex, string fragment)
    {
        var vert = Compile(ShaderType.VertexShader, vertex);
        var frag = 0;
        Handle = GL.CreateProgram();
        try
        {
            frag = Compile(ShaderType.FragmentShader, fragment);
            GL.AttachShader(Handle, vert);
            GL.AttachShader(Handle, frag);
            GL.LinkProgram(Handle);
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out var linked);
            if (linked == 0) throw new InvalidOperationException(GL.GetProgramInfoLog(Handle));
        }
        catch { GL.DeleteProgram(Handle); throw; }
        finally { GL.DeleteShader(vert); if (frag != 0) GL.DeleteShader(frag); }
    }

    public int this[string name] => uniforms.TryGetValue(name, out var location) ? location : uniforms[name] = GL.GetUniformLocation(Handle, name);
    public void Dispose() => GL.DeleteProgram(Handle);

    private static int Compile(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var compiled);
        if (compiled != 0) return shader;
        var error = GL.GetShaderInfoLog(shader);
        GL.DeleteShader(shader);
        throw new InvalidOperationException(error);
    }
}
