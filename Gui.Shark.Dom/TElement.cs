using AngleSharp.Extensions;
using AngleSharp.Services.Default;
using Gui.Shark.Dom.Extensions;
using Gui.Shark.Dom.Interfaces;
using Gui.Shark.Dom.Enums.Properties;
using Gui.Shark.Gfx.Factories;
using Gui.Shark.Gfx.Interfaces;
using System.Collections.Generic;
using AngleSharp.Css.Values;

namespace Gui.Shark.Dom
{
    public abstract class TElement : IElement
    {
        #region Public Properties

        protected Color DefaultBackgroundColor { get; set; }
        protected Color DefaultBorderColor { get; set; }
        protected Color DefaultForegroundColor { get; set; }

        public IGfxCanvas Canvas { get; set; }
        public IElementCss Css { get; set; }
        public IElementHtml Html { get; set; }

        public TRectangle BoundingBox { get; set; }

        public TPoint LeftFloatPosition;
        public TPoint RightFloatPosition;

        #region Tree Navigation

        public IElement Parent { get; set; }
        public IList<IElement> Children { get; set; }

        #endregion

        #endregion

        #region Private Properties

        private IList<IElement> normalFlowChildren;
        private IList<IElement> floatFlowChildren;
        private IList<IElement> absoluteFlowChildren;

        #endregion

        public TElement(AngleSharp.Dom.IElement htmlElement, IElement parent)
        {
            normalFlowChildren = new List<IElement>();
            floatFlowChildren = new List<IElement>();
            absoluteFlowChildren = new List<IElement>();

            DefaultBackgroundColor = new Color(255, 255, 255, 255);
            DefaultBorderColor = new Color(0, 0, 0, 255);
            DefaultForegroundColor = new Color(0, 0, 0, 255);

            Parent = parent;

            Children = new List<IElement>();
            Canvas = GfxFactory.Create<IGfxCanvas>();

            InitHtml(htmlElement);
            InitCss(htmlElement);

            ComputeBoundingBox();

            var elementFactory = new TElementFactory();

            foreach (AngleSharp.Dom.IElement htmlChild in htmlElement.Children)
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
            foreach (var child in normalFlowChildren)
            {
                child.Paint();
            }

            foreach (var child in floatFlowChildren)
            {
                child.Paint();
            }

            foreach (var child in absoluteFlowChildren)
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

        protected virtual void InitCss(AngleSharp.Dom.IElement htmlElement)
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

            var penColor = new Core.Gfx.TColor(Css.Color.R, Css.Color.G, Css.Color.B, Css.Color.A);
            var brushColor = new Core.Gfx.TColor(Css.BackgroundColor.R, Css.BackgroundColor.G, Css.BackgroundColor.B, Css.BackgroundColor.A);
            Canvas.Initialize(penColor, brushColor);
        }

        protected virtual void InitHtml(AngleSharp.Dom.IElement htmlElement)
        {
            Html = new TElementHtml
            {
                Text = htmlElement.Text()
            };
        }

        #endregion

        #region Private Methods

        private void ComputeBoundingBox(IElement element)
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

        private void AddToFlowList(IElement element)
        {
            switch (element.GetFloat())
            {
                case Float.None:
                    normalFlowChildren.Add(element);
                    break;
                case Float.Left:
                case Float.Right:
                    floatFlowChildren.Add(element);
                    break;
            }

            //TODO! Take element's Position into consideration too.
        }

        #endregion
    }
}
