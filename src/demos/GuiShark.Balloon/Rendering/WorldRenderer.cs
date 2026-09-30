using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GuiShark.Balloon;

/// <summary>Independent 3D renderer with no HTML or GuiShark dependencies.</summary>
internal sealed class WorldRenderer : IDisposable
{
    private readonly SceneShader shader = new();
    private readonly SceneMesh landscape;
    private readonly SceneMesh balloon = new(CharacterGeometry.Balloon());
    private readonly SceneMesh lantern = new(CharacterGeometry.Lantern());
    private readonly SceneMesh ring = new(CharacterGeometry.Ring());
    public WorldRenderer(Terrain terrain) => landscape = new(WorldGeometry.Build(terrain));
    public void Render(Expedition expedition, FollowCamera camera, int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        GL.Viewport(0, 0, width, height); GL.Disable(EnableCap.ScissorTest); GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest); GL.DepthMask(true); GL.Disable(EnableCap.CullFace);
        GL.ClearColor(.64f, .78f, .68f, 1); GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        shader.Begin(camera, expedition.Flight.Position); shader.Model(Matrix4.Identity, ground: true); landscape.Draw();
        DrawLanterns(expedition); DrawDestination(expedition);
        var velocity = expedition.Flight.Velocity;
        shader.Model(Matrix4.CreateRotationZ(-velocity.X * .014f) * Matrix4.CreateRotationX(velocity.Y * .014f) * Matrix4.CreateTranslation(expedition.Flight.Position));
        balloon.Draw();
    }
    private void DrawLanterns(Expedition expedition)
    {
        var i = 0;
        foreach (var light in expedition.Lanterns.Where(l => !l.Collected))
        {
            var p = expedition.Terrain.Point(light.Location.X, light.Location.Y, 9 + MathF.Sin(expedition.Time * 1.5f + i++) * .5f);
            shader.Model(Matrix4.CreateRotationY(expedition.Time * .25f) * Matrix4.CreateTranslation(p), emission: 1); lantern.Draw();
            shader.Model(Matrix4.CreateTranslation(expedition.Terrain.Point(light.Location.X, light.Location.Y, .16f)), emission: .7f); ring.Draw();
        }
    }
    private void DrawDestination(Expedition expedition)
    {
        if (expedition.Flight.Arrived) return;
        var target = expedition.Flight.Destination;
        shader.Model(Matrix4.CreateScale(1 + MathF.Sin(expedition.Time * 3) * .08f) * Matrix4.CreateTranslation(expedition.Terrain.Point(target.X, target.Y, .2f)), emission: .5f);
        ring.Draw();
    }
    public void Dispose() { landscape.Dispose(); balloon.Dispose(); lantern.Dispose(); ring.Dispose(); shader.Dispose(); }
}
