using OpenTK.Mathematics;

namespace GuiShark.Balloon;

/// <summary>Builds flat-shaded colored triangles in mesh-local coordinates.</summary>
internal sealed class MeshBuilder
{
    private readonly List<float> vertices = [];
    public float[] ToArray() => vertices.ToArray();
    public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 color)
    {
        var cross = Vector3.Cross(b - a, c - a);
        var normal = cross.LengthSquared > .00001f ? cross.Normalized() : Vector3.UnitY;
        Vertex(a, normal, color); Vertex(b, normal, color); Vertex(c, normal, color);
    }
    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 color)
    {
        Triangle(a, b, c, color); Triangle(a, c, d, color);
    }
    public void Box(Vector3 center, Vector3 size, Vector3 color)
    {
        var h = size / 2;
        Vector3 P(float x, float y, float z) => center + new Vector3(x * h.X, y * h.Y, z * h.Z);
        Quad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), color);
        Quad(P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), color);
        Quad(P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), color);
        Quad(P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), color);
        Quad(P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1), color);
        Quad(P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), color);
    }
    public void Cone(Vector3 bottom, float radius, float height, Vector3 color, int sides = 9, float topRadius = 0)
    {
        for (var i = 0; i < sides; i++)
        {
            var a = i * MathF.Tau / sides; var b = (i + 1) * MathF.Tau / sides;
            var pa = new Vector3(MathF.Cos(a), 0, MathF.Sin(a)); var pb = new Vector3(MathF.Cos(b), 0, MathF.Sin(b));
            Quad(bottom + pa * radius, bottom + Vector3.UnitY * height + pa * topRadius,
                bottom + Vector3.UnitY * height + pb * topRadius, bottom + pb * radius, color);
            Triangle(bottom, bottom + pa * radius, bottom + pb * radius, color);
        }
    }
    public void Ellipsoid(Vector3 center, Vector3 radius, Func<int, Vector3> color, int slices = 16, int bands = 10)
    {
        Vector3 Point(int i, int j)
        {
            var lat = -MathF.PI / 2 + j * MathF.PI / bands; var lon = i * MathF.Tau / slices;
            return center + new Vector3(MathF.Cos(lat) * MathF.Cos(lon) * radius.X, MathF.Sin(lat) * radius.Y, MathF.Cos(lat) * MathF.Sin(lon) * radius.Z);
        }
        for (var i = 0; i < slices; i++)
            for (var j = 0; j < bands; j++) Quad(Point(i, j), Point(i, j + 1), Point(i + 1, j + 1), Point(i + 1, j), color(i));
    }
    public void Rod(Vector3 start, Vector3 end, float radius, Vector3 color)
    {
        var axis = (end - start).Normalized();
        var u = Vector3.Cross(axis, MathF.Abs(axis.Y) > .9f ? Vector3.UnitX : Vector3.UnitY).Normalized() * radius;
        var v = Vector3.Cross(axis, u).Normalized() * radius;
        for (var i = 0; i < 6; i++)
        {
            var a = u * MathF.Cos(i * MathF.Tau / 6) + v * MathF.Sin(i * MathF.Tau / 6);
            var b = u * MathF.Cos((i + 1) * MathF.Tau / 6) + v * MathF.Sin((i + 1) * MathF.Tau / 6);
            Quad(start + a, end + a, end + b, start + b, color);
        }
    }
    private void Vertex(Vector3 p, Vector3 n, Vector3 c) => vertices.AddRange([p.X, p.Y, p.Z, n.X, n.Y, n.Z, c.X, c.Y, c.Z]);
}
