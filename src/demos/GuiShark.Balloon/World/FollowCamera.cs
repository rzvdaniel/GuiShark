using OpenTK.Mathematics;

namespace GuiShark.Balloon;

internal sealed class FollowCamera
{
    private Vector3 focus;
    public Matrix4 View { get; private set; }
    public Matrix4 Projection { get; private set; }
    public Vector3 Eye { get; private set; }
    public float Zoom { get; private set; } = 54;

    public void Reset(Vector3 balloon) => focus = balloon + new Vector3(0, -3, 0);
    public void AdjustZoom(float wheel) => Zoom = Math.Clamp(Zoom - wheel * 3, 38, 78);

    public void Update(Vector3 balloon, float dt, float aspect)
    {
        var target = balloon + new Vector3(0, -3, 0);
        focus = Vector3.Lerp(focus, target, 1 - MathF.Exp(-2.5f * dt));
        Eye = focus + new Vector3(-30, 36, 38);
        View = Matrix4.LookAt(Eye, focus, Vector3.UnitY);
        Projection = Matrix4.CreateOrthographic(Zoom * aspect, Zoom, .1f, 400);
    }

    public Vector2? PickTerrain(float x, float y, float width, float height, Terrain terrain)
    {
        if (width <= 0 || height <= 0) return null;
        var inverse = Matrix4.Invert(View * Projection);
        var near = Unproject(new(2 * x / width - 1, 1 - 2 * y / height, -1, 1), inverse);
        var far = Unproject(new(2 * x / width - 1, 1 - 2 * y / height, 1, 1), inverse);
        var direction = (far - near).Normalized();
        for (float distance = 0; distance < 400; distance += .5f)
        {
            var point = near + direction * distance;
            if (MathF.Abs(point.X) > Terrain.FlightBoundary || MathF.Abs(point.Z) > Terrain.FlightBoundary) continue;
            if (point.Y <= terrain.Height(point.X, point.Z)) return new(point.X, point.Z);
        }
        return null;
    }

    private static Vector3 Unproject(Vector4 point, Matrix4 inverse)
    {
        var world = point * inverse;
        return world.Xyz / world.W;
    }
}
