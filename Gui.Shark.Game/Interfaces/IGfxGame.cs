using Gui.Shark.Dom.Interfaces;

namespace Gui.Shark.Gfx.Interfaces
{
    public interface IGfxGame
    {
        #region Properties

        IScreen Screen { get; set; }

        #endregion

        #region Methods

        void Run();
        void Run(double updateRate);
        void Run(double updatesPerSecond, double framesPerSecond);
        void Create(int width, int height, int windowsFlag, string htmlDocument);

        #endregion
    }
}
