using Gui.Shark.Core.Gfx;
using Gui.Shark.Gfx.Drawing;

namespace Gui.Shark.Gfx.Interfaces
{
    public interface IGfxCanvas
    {
        void Initialize(TColor penColor, TColor brushColor);

        TPoint PenPos { get; set; }
        TBrush Brush { get; set; }
        TPen Pen { get; set; }

        void DrawRect(float x, float y, float width, float height, int border);
        void DrawRect(TRectangle rect, int border);

        void FillRect(float x, float y, float width, float height);
        void FillRect(TRectangle rect);

        void MoveTo(float x, float y);
        void LineTo(float x, float y);

        void Print(string text, TPoint position);
        void Print(string text, float x, float y);
    }
}
