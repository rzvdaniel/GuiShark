using OpenTK.Mathematics;

namespace GuiShark.Balloon;

/// <summary>Seeded continuous hills, shared by mesh generation, flight height and mouse picking.</summary>
internal sealed class Terrain(int seed)
{
    public const float FlightBoundary = 66;
    public float Height(float x, float z)
    {
        var rolling = 2 + 9 * Noise(x * .028f, z * .028f) + 1.4f * Noise(x * .08f, z * .08f)
            + MathF.Sin(x * .06f + z * .035f) * 1.8f;
        var lake = MathF.Exp(-((x + 27) * (x + 27) + (z - 28) * (z - 28)) / 140);
        var ridge = Math.Max(0, (MathF.Abs(x) + MathF.Abs(z) - 100) / 12);
        return rolling - lake * 6 + ridge * ridge * 1.4f;
    }

    public Vector3 Point(float x, float z, float above = 0) => new(x, Height(x, z) + above, z);

    private float Noise(float x, float z)
    {
        var ix = (int)MathF.Floor(x);
        var iz = (int)MathF.Floor(z);
        var tx = Smooth(x - ix);
        var tz = Smooth(z - iz);
        return Lerp(Lerp(Hash(ix, iz), Hash(ix + 1, iz), tx), Lerp(Hash(ix, iz + 1), Hash(ix + 1, iz + 1), tx), tz);
    }

    private float Hash(int x, int z)
    {
        var n = unchecked((uint)(x * 374761393 + z * 668265263 + seed * 1447));
        n = (n ^ (n >> 13)) * 1274126177u;
        return (n ^ (n >> 16)) / (float)uint.MaxValue;
    }

    private static float Smooth(float t) => t * t * (3 - 2 * t);
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
