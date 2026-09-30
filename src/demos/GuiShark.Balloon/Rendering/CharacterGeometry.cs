using OpenTK.Mathematics;

namespace GuiShark.Balloon;

internal static class CharacterGeometry
{
    public static float[] Balloon()
    {
        var mesh = new MeshBuilder();
        var cream = new Vector3(.98f, .89f, .68f); var coral = new Vector3(.88f, .30f, .18f); var teal = new Vector3(.16f, .48f, .48f);
        mesh.Ellipsoid(new(0, 5.5f, 0), new(2.5f, 3.2f, 2.5f), i => ((i / 2) % 3) switch { 0 => coral, 1 => cream, _ => teal }, 24, 16);
        mesh.Cone(new(0, 1.8f, 0), .55f, 1.4f, coral, 16, .95f);
        mesh.Box(new(0, .4f, 0), new(1.2f, .8f, 1), new(.58f, .36f, .17f));
        mesh.Box(new(0, .8f, 0), new(1.32f, .16f, 1.12f), new(.83f, .6f, .32f));
        foreach (var x in new[] { -.5f, .5f })
            foreach (var z in new[] { -.4f, .4f }) mesh.Rod(new(x, .8f, z), new(x * 1.3f, 2.8f, z * 1.3f), .035f, new(.92f, .79f, .53f));
        mesh.Cone(new(0, .9f, 0), .18f, .8f, new(1, .55f, .1f), 7);
        return mesh.ToArray();
    }
    public static float[] Lantern()
    {
        var mesh = new MeshBuilder();
        mesh.Ellipsoid(Vector3.Zero, new(.7f, 1, .7f), _ => new(1, .76f, .26f), 8, 6);
        mesh.Cone(new(0, -1.2f, 0), .24f, .3f, new(.65f, .35f, .13f), 8, .24f);
        mesh.Cone(new(0, 1, 0), .28f, .2f, new(1, .92f, .65f), 8);
        return mesh.ToArray();
    }
    public static float[] Ring()
    {
        var mesh = new MeshBuilder();
        for (var i = 0; i < 40; i++)
        {
            var a = i * MathF.Tau / 40; var b = (i + 1) * MathF.Tau / 40;
            Vector3 P(float angle, float radius) => new(MathF.Cos(angle) * radius, 0, MathF.Sin(angle) * radius);
            mesh.Quad(P(a, 1.6f), P(b, 1.6f), P(b, 2), P(a, 2), new(1, .83f, .39f));
        }
        return mesh.ToArray();
    }
}
