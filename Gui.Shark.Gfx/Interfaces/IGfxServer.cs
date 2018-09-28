using Gui.Shark.Core.Gfx;

namespace Gui.Shark.Gfx.Interfaces
{
    public interface IGfxServer
    {
        int Width { get; set; }
        int Height { get; set; }
        TColor ClearColor { get; set; }

        void Initialize(int width, int height, TColor clearColor);

        void Begin();
	    void End();
	
	    void BeginScene();
	    void EndScene();
    }
}
