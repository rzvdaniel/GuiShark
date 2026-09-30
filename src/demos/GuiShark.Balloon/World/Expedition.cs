using OpenTK.Mathematics;

namespace GuiShark.Balloon;

internal sealed class Lantern(Vector2 location)
{
    public Vector2 Location { get; } = location;
    public bool Collected { get; set; }
}

internal sealed class Expedition
{
    public Terrain Terrain { get; } = new(73);
    public Flight Flight { get; }
    public IReadOnlyList<Lantern> Lanterns { get; } = new Lantern[]
    {
        new(new(-16, -12)), new(new(-3, -8)), new(new(12, 0)),
        new(new(24, 13)), new(new(7, 25)), new(new(-14, 14))
    };
    public float Time { get; private set; }
    public int Collected => Lanterns.Count(l => l.Collected);
    public bool Complete => Collected == Lanterns.Count;
    public string Message { get; private set; } = "The valley is yours. Follow the floating golden lanterns.";

    public Expedition() { Flight = new(Terrain); Reset(); }

    public void Reset()
    {
        Flight.Reset();
        Time = 0;
        foreach (var lantern in Lanterns) lantern.Collected = false;
        Message = "The valley is yours. Follow the floating golden lanterns.";
    }

    public void Update(float dt)
    {
        Time += dt;
        Flight.Update(dt, Time);
        foreach (var lantern in Lanterns.Where(l => !l.Collected))
            if ((lantern.Location - new Vector2(Flight.Position.X, Flight.Position.Z)).Length < 2.8f)
            {
                lantern.Collected = true;
                Message = Complete ? "Every light found. A beautiful little journey." : $"Lantern {Collected} found. Keep drifting; there's more to discover.";
                Console.WriteLine(Message);
            }
    }

    public void GuideToNext()
    {
        var position = new Vector2(Flight.Position.X, Flight.Position.Z);
        var next = Lanterns.Where(l => !l.Collected).MinBy(l => (l.Location - position).LengthSquared);
        if (next == null) return;
        Flight.SetDestination(next.Location);
        Message = "Course set for the nearest lantern. Enjoy the view.";
    }
}
