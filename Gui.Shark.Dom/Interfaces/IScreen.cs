using Gui.Shark.Core.Gfx;
using Gui.Shark.Gfx.Interfaces;

namespace Gui.Shark.Dom.Interfaces
{
    public interface IScreen
    {
        IGfxServer GfxServer { get; set; }
        IGfxCanvas Canvas { get; set; }
        IDocument Document { get; set; }

        void Create(int width, int height, TColor clearColor, string htmlDocument);
        void Paint();
    }
}
