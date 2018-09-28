using Gui.Shark.Core.Gfx;
using Gui.Shark.Dom.Factories;
using Gui.Shark.Dom.Interfaces;
using Gui.Shark.Gfx.Factories;
using Gui.Shark.Gfx.Interfaces;

namespace Gui.Shark.Dom
{
    public class TScreen : IScreen
    {
        public static int Width { get; private set; }
        public static int Height { get; private set; }

        public IGfxServer GfxServer { get; set; }
        public IGfxCanvas Canvas { get; set; }
        public IDocument Document { get; set; }

        public virtual void Create(int width, int height, TColor clearColor, string htmlDocument)
        {
            Canvas = GfxFactory.Create<IGfxCanvas>();
            GfxServer = GfxFactory.Create<IGfxServer>();
            GfxServer.Initialize(width, height, clearColor);

            Width = width;
            Height = height;

            var domFactory = new TDomFactory();
            Document = domFactory.Create<IDocument>();
            Document.Parse(htmlDocument);

            // Create the canvas. Need a canvas for the screen. For example,
            // the screen render the mouse cursor. Because it render something, 
            // we need a Canvas. 
            // As you can see, this is a hack, because, normally, every
            // control create it's own canvas in TControl::Create(). But
            // we skip the base class initialization, so need to create the
            // canvas by hand.

            //Canvas = new TCanvas(this);

            //SetBounds(Rect.left, Rect.top, Rect.right, Rect.bottom);#
        }

        public void Paint()
        {
            GfxServer.Begin();

            Document.Body.Paint();

            GfxServer.End();
        }
    }
}
