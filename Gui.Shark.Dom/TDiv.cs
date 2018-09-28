﻿using AngleSharp.Dom;
using CssValues = AngleSharp.Css.Values;

namespace Gui.Shark.Dom
{
    public class TDiv : TElement
    {
        public TDiv(IElement htmlElement, TElement parent)
            : base(htmlElement, parent)
        {
        }

        public override void PaintBody()
        {
            Canvas.FillRect(BoundingBox);
        }

        protected override void InitCss(IElement htmlElement)
        {
            base.InitCss(htmlElement);

            if (Css.Width.Value == 0.0f)
            {
                Css.Width = new CssValues.Length(TScreen.Width, CssValues.Length.Unit.None);
            }
        }
    }
}
