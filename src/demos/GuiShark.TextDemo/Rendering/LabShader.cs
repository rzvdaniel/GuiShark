using OpenTK.Graphics.OpenGL4;

namespace GuiShark.TextDemo;

internal sealed class LabShader : IDisposable
{
    public int Handle { get; }
    public int this[string name] => GL.GetUniformLocation(Handle, name);
    public LabShader(string vertexSource, string fragmentSource)
    {
        var vertex = Compile(ShaderType.VertexShader, vertexSource);
        var fragment = Compile(ShaderType.FragmentShader, fragmentSource);
        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertex); GL.AttachShader(Handle, fragment); GL.LinkProgram(Handle);
        GL.DeleteShader(vertex); GL.DeleteShader(fragment);
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out var linked);
        if (linked == 0) throw new InvalidOperationException(GL.GetProgramInfoLog(Handle));
    }
    private static int Compile(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source); GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var compiled);
        if (compiled == 0) throw new InvalidOperationException(GL.GetShaderInfoLog(shader));
        return shader;
    }
    public void Dispose() => GL.DeleteProgram(Handle);
}
