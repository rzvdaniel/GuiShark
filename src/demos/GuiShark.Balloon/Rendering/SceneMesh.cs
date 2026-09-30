using OpenTK.Graphics.OpenGL4;

namespace GuiShark.Balloon;

internal sealed class SceneMesh : IDisposable
{
    private readonly int vao = GL.GenVertexArray();
    private readonly int vbo = GL.GenBuffer();
    private readonly int count;
    public SceneMesh(float[] vertices)
    {
        count = vertices.Length / 9;
        GL.BindVertexArray(vao); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        for (var i = 0; i < 3; i++)
        {
            GL.EnableVertexAttribArray(i);
            GL.VertexAttribPointer(i, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), i * 3 * sizeof(float));
        }
    }
    public void Draw() { GL.BindVertexArray(vao); GL.DrawArrays(PrimitiveType.Triangles, 0, count); }
    public void Dispose() { GL.DeleteBuffer(vbo); GL.DeleteVertexArray(vao); }
}
