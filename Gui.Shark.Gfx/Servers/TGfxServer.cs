using Gui.Shark.Core.Gfx;
using Gui.Shark.Gfx.Interfaces;

namespace Gui.Shark.Gfx.Servers
{
    public abstract class TGfxServer : IGfxServer
    {
        public TColor ClearColor { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public void Initialize(int width, int height, TColor clearColor)
        {
            Width = width;
            Height = height;
            ClearColor = clearColor;
        }

        public abstract void Begin();
        public abstract void End();

        public abstract void BeginScene();
        public abstract void EndScene();
    }
}
