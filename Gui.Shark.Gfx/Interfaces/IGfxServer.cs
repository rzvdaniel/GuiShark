using OpenTK;

namespace Gui.Shark.Gfx.Interfaces
{
    public interface IGfxServer
    {
        int Width { get; set; }
        int Height { get; set; }
        Color ClearColor { get; set; }

        void Initialize(int width, int height, Color clearColor);

        void Begin();
	    void End();
	
	    void BeginScene();
	    void EndScene();
    }
}
