using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.Extensions;
using AngleSharp.Services.Default;
using Gui.Shark.Core.Gfx;
using Gui.Shark.Dom.Enums.Properties;
using Gui.Shark.Dom.Extensions;
using Gui.Shark.Gfx.Factories;
using Gui.Shark.Gfx.Interfaces;
using System.Collections.Generic;

namespace Gui.Shark.Dom
{
    public abstract class TElement 
    {
        #region Public Properties

        protected Color DefaultBackgroundColor { get; set; }
        protected Color DefaultBorderColor { get; set; }
        protected Color DefaultForegroundColor { get; set; }

        public IGfxCanvas Canvas { get; set; }
        public TElementCss Css { get; set; }
        public TElementHtml Html { get; set; }

        public TRectangle BoundingBox { get; set; }
        public TPoint LeftFloatPosition;
        public TPoint RightFloatPosition;

        #region Tree Navigation

        public TElement Parent { get; set; }
        public IList<TElement> Children { get; set; }

        #endregion

        #endregion

        #region Private Properties

        private IList<TElement> NormalFlowChildren { get; set; }
        private IList<TElement> FloatFlowChildren { get; set; }
        private IList<TElement> AbsoluteFlowChildren { get; set; }

        #endregion

        public TElement(IElement htmlElement, TElement parent)
        {
            NormalFlowChildren = new List<TElement>();
            FloatFlowChildren = new List<TElement>();
            AbsoluteFlowChildren = new List<TElement>();

            DefaultBackgroundColor = new Color(255, 255, 255, 255);
            DefaultBorderColor = new Color(0, 0, 0, 255);
            DefaultForegroundColor = new Color(0, 0, 0, 255);

            Parent = parent;
            Children = new List<TElement>();
            Canvas = GfxFactory.Create<IGfxCanvas>();

            InitHtml(htmlElement);
            InitCss(htmlElement);
            ComputeBoundingBox();

            var elementFactory = new TElementFactory();

            foreach (IElement htmlChild in htmlElement.Children)
            {
                var child = elementFactory.Create(htmlChild, this);

                Children.Add(child);
                ComputeBoundingBox(child);
                AddToFlowList(child);
            }
        }

        public virtual void ComputeBoundingBox()
        {
            LeftFloatPosition = TPoint.Zero;
            RightFloatPosition = new TPoint(Parent.BoundingBox.Width, 0);
        }

        /// <summary>
        /// Renders element's children
        /// </summary>
        public virtual void PaintChildren()
        {
            foreach (var child in NormalFlowChildren)
            {
                child.Paint();
            }

            foreach (var child in FloatFlowChildren)
            {
                child.Paint();
            }

            foreach (var child in AbsoluteFlowChildren)
            {
                child.Paint();
            }
        }

        /// <summary>
        /// Renders element's body
        /// </summary>
        /// <remarks>
        /// Elements are responsible to render their own body.
        /// There is no default body painting.
        /// </remarks>
        public virtual void PaintBody() {}

        /// <summary>
        /// Renders element's text
        /// </summary>
        public virtual void PaintText()
        {
            Canvas.Print(Html.Text, BoundingBox.Left, BoundingBox.Top);
        }

        /// <summary>
        /// Paints element
        /// </summary>
        public void Paint()
        {
            PaintChildren();

            PaintBody();

            PaintText();
        }

        #region Protected Methods

        protected virtual void InitCss(IElement htmlElement)
        {
            var style = htmlElement.ComputeCurrentStyle();

            Css = new TElementCss()
            {
                Width = style.Width.GetLength(),
                Height = style.Height.GetLength(),

                MaxWidth = style.MaxWidth.GetLength(),
                MaxHeight = style.MaxHeight.GetLength(),

                MinWidth = style.MinWidth.GetLength(),
                MinHeight = style.MinHeight.GetLength(),

                MarginTop = style.MarginTop.GetLength(),
                MarginBottom = style.MarginBottom.GetLength(),
                MarginLeft = style.MarginLeft.GetLength(),
                MarginRight = style.MarginRight.GetLength(),

                PaddingTop = style.PaddingTop.GetLength(),
                PaddingBottom = style.PaddingBottom.GetLength(),
                PaddingLeft = style.PaddingLeft.GetLength(),
                PaddingRight = style.PaddingRight.GetLength(),

                Float = style.Float,

                BackgroundColor = style.BackgroundColor.TryGetColor(DefaultBackgroundColor),
                Color = style.Color.TryGetColor(DefaultForegroundColor),

                FontFamily = style.FontFamily
            };

            var penColor = new TColor(Css.Color.R, Css.Color.G, Css.Color.B, Css.Color.A);
            var brushColor = new TColor(Css.BackgroundColor.R, Css.BackgroundColor.G, Css.BackgroundColor.B, Css.BackgroundColor.A);

            Canvas.Initialize(penColor, brushColor);
        }

        protected virtual void InitHtml(IElement htmlElement)
        {
            Html = new TElementHtml
            {
                Text = htmlElement.Text()
            };
        }

        #endregion

        #region Private Methods

        private void ComputeBoundingBox(TElement element)
        {
            var box = new TRectangle()
            {
                Width = element.Css.Width.Value,
                Height = element.Css.Height.Value
            };

            var childFloatAttribute = element.GetFloat();

            if (childFloatAttribute == Float.None)
            {
                box.X = BoundingBox.Left;
                box.Y = LeftFloatPosition.Y;

                LeftFloatPosition.X = BoundingBox.Left;
                LeftFloatPosition.Y += box.Height;
                RightFloatPosition.Y += box.Height;
            }
            else if (childFloatAttribute == Float.Left)
            {
                box.X = LeftFloatPosition.X;
                box.Y = LeftFloatPosition.Y;

                LeftFloatPosition.X += box.Width;

                if (LeftFloatPosition.X + box.Width > BoundingBox.Width)
                {
                    LeftFloatPosition.X = BoundingBox.Left;
                    LeftFloatPosition.Y += box.Height;
                }
            }
            else if (childFloatAttribute == Float.Right)
            {
                box.X = RightFloatPosition.X - box.Width;
                box.Y = RightFloatPosition.Y;

                RightFloatPosition.X -= box.Width;

                if (RightFloatPosition.X < BoundingBox.Left)
                {
                    RightFloatPosition.X = BoundingBox.Width;
                    RightFloatPosition.Y += box.Height;
                }
            }

            element.BoundingBox = box;
        }

        private void AddToFlowList(TElement element)
        {
            switch (element.GetFloat())
            {
                case Float.None:
                    NormalFlowChildren.Add(element);
                    break;
                case Float.Left:
                case Float.Right:
                    FloatFlowChildren.Add(element);
                    break;
            }

            //TODO! Take element's Position into consideration too.
        }

        #endregion
    }
}
