using Gui.Shark.Core.Gfx;
using Gui.Shark.Dom;
using Gui.Shark.Dom.Interfaces;
using Gui.Shark.Gfx.Interfaces;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System;

namespace Gui.Shark.Gfx.OpenGL
{
    public class TGame : IGfxGame
    {
        #region Properties

        private GameWindow Window { get; set; }
        public IScreen Screen { get; set; }

        #endregion

        #region Methods

        public void Create(int width, int height, int windowsFlag, string html)
        {
            Screen = new TScreen();
            Screen.Create(width, height, TColor.White, html);

            Window = new GameWindow(width, height, GraphicsMode.Default, "Gui Sharp Samples", (GameWindowFlags)windowsFlag)
            {
                VSync = VSyncMode.On
            };

            Window.Load += OnLoad;
            Window.Resize += OnResize;
            Window.UpdateFrame += OnUpdateFrame;
            Window.RenderFrame += OnRenderFrame;
            Window.Disposed += OnDispose;
        }

        /// <summary>
        ///  Enters the game loop of the GameWindow updating and rendering at the specified
        ///  frequency.
        /// </summary>
        /// <param name="updatesPerSecond">The frequency of UpdateFrame events.</param>
        /// <param name="framesPerSecond">The frequency of RenderFrame events.</param>
        /// <remarks>
        ///  When overriding the default game loop you should call ProcessEvents() to ensure
        ///  hat your GameWindow responds to operating system events.
        ///  Once ProcessEvents() returns, it is time to call update and render the next frame.
        /// </remarks>
        public void Run(double updatesPerSecond, double framesPerSecond)
        {
            Window.Run(updatesPerSecond, framesPerSecond);
        }

        /// <summary>
        ///  Enters the game loop of the GameWindow using the specified update rate.maximum
        /// possible render frequency.
        /// </summary>
        /// <param name="updateRate"></param>
        public void Run(double updateRate)
        {
            Window.Run(updateRate);
        }

        /// <summary>
        /// Enters the game loop of the GameWindow using the maximum update rate.
        /// </summary>
        public void Run()
        {
            Window.Run();
        }

        #endregion

        #region Event Handlers

        private void OnLoad(object sender, EventArgs e)
        {
        }

        private void OnRenderFrame(object sender, FrameEventArgs e)
        {
            Screen.Paint();

            Window.SwapBuffers();
        }

        private void OnUpdateFrame(object sender, FrameEventArgs e)
        {
            if (Window.Keyboard[Key.Escape])
                Window.Exit();
        }

        private void OnResize(object sender, EventArgs e)
        {
        }

        private void OnDispose(object sender, EventArgs e)
        {
        }

        #endregion
    }
}
