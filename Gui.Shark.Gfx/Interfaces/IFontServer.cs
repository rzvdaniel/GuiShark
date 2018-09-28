namespace Gui.Shark.Gfx.Interfaces
{
    public interface IFontServer
    {
        void RenderText(string text, TPoint position);

        void RenderText(string text, float x, float y);
    }
}
