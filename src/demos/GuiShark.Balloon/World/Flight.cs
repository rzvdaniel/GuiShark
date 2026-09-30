using OpenTK.Mathematics;

namespace GuiShark.Balloon;

internal sealed class Flight(Terrain terrain)
{
    public Vector3 Position { get; private set; }
    public Vector2 Destination { get; private set; }
    public Vector2 Velocity { get; private set; }
    public float DistanceTravelled { get; private set; }
    public float Altitude => Position.Y - terrain.Height(Position.X, Position.Z);
    public bool Arrived => (new Vector2(Position.X, Position.Z) - Destination).Length < .5f;

    public void Reset()
    {
        Destination = new(-23, -19);
        Position = terrain.Point(Destination.X, Destination.Y, 7);
        Velocity = Vector2.Zero;
        DistanceTravelled = 0;
    }

    public void SetDestination(Vector2 target) => Destination = new(
        Math.Clamp(target.X, -Terrain.FlightBoundary, Terrain.FlightBoundary),
        Math.Clamp(target.Y, -Terrain.FlightBoundary, Terrain.FlightBoundary));

    public void Update(float dt, float time)
    {
        var current = new Vector2(Position.X, Position.Z);
        var delta = Destination - current;
        var desired = delta.Length < .12f ? Vector2.Zero : delta.Normalized() * Math.Min(9, delta.Length * 1.6f);
        Velocity = Vector2.Lerp(Velocity, desired, 1 - MathF.Exp(-3 * dt));
        var drift = new Vector2(MathF.Sin(time * .6f), MathF.Cos(time * .4f)) * .12f;
        var movement = (Velocity + drift) * dt;
        current += movement;
        DistanceTravelled += movement.Length;
        var height = terrain.Height(current.X, current.Y) + 7 + MathF.Sin(time * 1.3f) * .3f;
        Position = new(current.X, Position.Y + (height - Position.Y) * (1 - MathF.Exp(-2 * dt)), current.Y);
    }
}
