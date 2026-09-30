using OpenTK.Mathematics;

namespace GuiShark.Balloon;

internal static class WorldGeometry
{
    public static float[] Build(Terrain terrain)
    {
        var mesh = new MeshBuilder(); var random = new Random(73);
        AddHills(mesh, terrain, random); AddLake(mesh);
        for (var i = 0; i < 460; i++)
        {
            var x = random.NextSingle() * 180 - 90; var z = random.NextSingle() * 180 - 90;
            if (new Vector2(x + 27, z - 28).Length < 13 || new Vector2(x + 20, z + 16).Length < 12) continue;
            AddTree(mesh, terrain.Point(x, z), 1.3f + random.NextSingle() * 1.8f, random.Next(3));
        }
        AddCottage(mesh, terrain.Point(5, 16)); AddCottage(mesh, terrain.Point(10, 19)); AddWindmill(mesh, terrain.Point(-31, -4));
        for (var i = 0; i < 90; i++)
        {
            var x = random.NextSingle() * 120 - 60; var z = random.NextSingle() * 120 - 60;
            mesh.Ellipsoid(terrain.Point(x, z, .2f), new(.8f, .45f, .6f), _ => new(.49f, .55f, .44f), 6, 4);
        }
        return mesh.ToArray();
    }
    private static void AddHills(MeshBuilder mesh, Terrain terrain, Random random)
    {
        for (float x = -110; x < 110; x += 2)
            for (float z = -110; z < 110; z += 2)
            {
                var variation = .94f + MathF.Sin(x * .045f) * MathF.Cos(z * .06f) * .08f + random.NextSingle() * .025f;
                var color = new Vector3(.41f, .62f, .27f) * variation;
                mesh.Quad(terrain.Point(x, z), terrain.Point(x, z + 2), terrain.Point(x + 2, z + 2), terrain.Point(x + 2, z), color);
            }
    }
    private static void AddLake(MeshBuilder mesh)
    {
        var center = new Vector3(-27, 2.9f, 28);
        for (var i = 0; i < 48; i++)
        {
            var a = i * MathF.Tau / 48; var b = (i + 1) * MathF.Tau / 48;
            mesh.Triangle(center, center + new Vector3(MathF.Cos(b) * 10, 0, MathF.Sin(b) * 8),
                center + new Vector3(MathF.Cos(a) * 10, 0, MathF.Sin(a) * 8), new(.27f, .66f, .69f));
        }
    }
    private static void AddTree(MeshBuilder mesh, Vector3 p, float h, int type)
    {
        mesh.Cone(p, .16f, h * .6f, new(.38f, .27f, .16f), 6, .12f);
        if (type == 0)
        {
            mesh.Cone(p + new Vector3(0, h * .35f, 0), h * .46f, h, new(.17f, .36f, .24f));
            mesh.Cone(p + new Vector3(0, h * .8f, 0), h * .34f, h * .75f, new(.22f, .45f, .29f));
        }
        else mesh.Ellipsoid(p + new Vector3(0, h, 0), new(h * .62f, h * .78f, h * .62f), _ => type == 1 ? new(.48f, .62f, .27f) : new(.29f, .49f, .26f), 8, 5);
    }
    private static void AddCottage(MeshBuilder mesh, Vector3 p)
    {
        mesh.Box(p + new Vector3(0, .9f, 0), new(2.8f, 1.8f, 2.4f), new(.91f, .81f, .61f));
        var roof = new Vector3(.66f, .31f, .21f);
        mesh.Quad(p + new Vector3(-1.7f, 1.8f, -1.5f), p + new Vector3(-1.7f, 1.8f, 1.5f), p + new Vector3(0, 3, 1.5f), p + new Vector3(0, 3, -1.5f), roof);
        mesh.Quad(p + new Vector3(0, 3, -1.5f), p + new Vector3(0, 3, 1.5f), p + new Vector3(1.7f, 1.8f, 1.5f), p + new Vector3(1.7f, 1.8f, -1.5f), roof);
        mesh.Box(p + new Vector3(.7f, 1.1f, 1.22f), new(.65f, .7f, .1f), new(.21f, .36f, .35f));
        mesh.Box(p + new Vector3(-.5f, .55f, 1.22f), new(.6f, 1.1f, .1f), new(.35f, .25f, .16f));
    }
    private static void AddWindmill(MeshBuilder mesh, Vector3 p)
    {
        mesh.Cone(p, 1.3f, 5, new(.85f, .79f, .62f), 8, .7f);
        mesh.Cone(p + new Vector3(0, 5, 0), 1.2f, 1.4f, new(.48f, .29f, .22f), 8);
        mesh.Rod(p + new Vector3(-2.4f, 2.7f, 1), p + new Vector3(2.4f, 6.1f, 1), .15f, new(.92f, .86f, .7f));
        mesh.Rod(p + new Vector3(-1.7f, 6.7f, 1), p + new Vector3(1.7f, 2, 1), .15f, new(.92f, .86f, .7f));
    }
}
