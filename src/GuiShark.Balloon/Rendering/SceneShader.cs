using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GuiShark.Balloon;

internal sealed class SceneShader : IDisposable
{
    public int Handle { get; }
    private readonly Dictionary<string, int> uniforms = [];
    private int Location(string name) => uniforms.TryGetValue(name, out var value) ? value : uniforms[name] = GL.GetUniformLocation(Handle, name);
    public SceneShader()
    {
        var vertex = Compile(ShaderType.VertexShader, """
            #version 330 core
            layout(location=0) in vec3 position;
            layout(location=1) in vec3 normal;
            layout(location=2) in vec3 color;
            uniform mat4 model, view, projection;
            out vec3 world, surfaceNormal, surfaceColor;
            void main() {
                world = (model * vec4(position,1)).xyz;
                surfaceNormal = mat3(model) * normal;
                surfaceColor = color;
                gl_Position = projection * view * vec4(world,1);
            }
            """);
        var fragment = Compile(ShaderType.FragmentShader, """
            #version 330 core
            in vec3 world, surfaceNormal, surfaceColor;
            out vec4 color;
            uniform vec3 eye, balloon;
            uniform float emission;
            uniform bool ground;
            void main() {
                vec3 n = normalize(surfaceNormal);
                float light = .48 + .52 * max(0.,dot(n,normalize(vec3(-.5,1.,.4))));
                vec3 lit = surfaceColor * mix(light,1.12,emission);
                if (ground) {
                    vec2 delta = (world.xz - balloon.xz) / vec2(3.5,2.8);
                    lit *= 1. - .36*exp(-dot(delta,delta)*1.5);
                }
                float fog = smoothstep(78.,155.,distance(eye,world));
                color = vec4(mix(lit,vec3(.64,.78,.68),fog),1);
            }
            """);
        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertex); GL.AttachShader(Handle, fragment); GL.LinkProgram(Handle);
        GL.DeleteShader(vertex); GL.DeleteShader(fragment);
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out var success);
        if (success == 0) throw new InvalidOperationException(GL.GetProgramInfoLog(Handle));
    }
    public void Begin(FollowCamera camera, Vector3 balloon)
    {
        GL.UseProgram(Handle); Matrix("view", camera.View); Matrix("projection", camera.Projection);
        GL.Uniform3(Location("eye"), camera.Eye); GL.Uniform3(Location("balloon"), balloon);
    }
    public void Model(Matrix4 model, bool ground = false, float emission = 0)
    {
        Matrix("model", model); GL.Uniform1(Location("ground"), ground ? 1 : 0); GL.Uniform1(Location("emission"), emission);
    }
    private void Matrix(string name, Matrix4 value) => GL.UniformMatrix4(Location(name), false, ref value);
    private static int Compile(ShaderType type, string source)
    {
        var handle = GL.CreateShader(type); GL.ShaderSource(handle, source); GL.CompileShader(handle);
        GL.GetShader(handle, ShaderParameter.CompileStatus, out var success);
        if (success != 0) return handle;
        var error = GL.GetShaderInfoLog(handle); GL.DeleteShader(handle); throw new InvalidOperationException(error);
    }
    public void Dispose() => GL.DeleteProgram(Handle);
}
