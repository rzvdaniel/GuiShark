using Gui.Shark.Gfx.Interfaces;
using Gui.Shark.Dom.Interfaces;
using System.Collections.Generic;

namespace Gui.Shark.Dom.Interfaces
{
    public interface IElement
    {
        #region Properties

        IElement Parent { get; set; }
        IList<IElement> Children { get; set; }

        IGfxCanvas Canvas { get; set; }
        IElementCss Css { get; set; }
        IElementHtml Html { get; set; }

        TRectangle BoundingBox { get; set; }

        #endregion

        void Paint();
        void PaintChildren();
        void PaintText();
        void PaintBody();

        void ComputeBoundingBox();
    }
}
