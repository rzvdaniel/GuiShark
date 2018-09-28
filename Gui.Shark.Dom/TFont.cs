using OpenTK;

namespace Gui.Shark.Dom
{
    public class TFont
    {
        /// <summary>
        /// Font family name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The font colour
        /// </summary>
        public Color Colour { get; set; }

        /// <summary>
        /// Spacing between characters in units of average glyph width
        /// </summary>
        public float CharacterSpacing { get; set; }

        /// <summary>
        /// Spacing between words in units of average glyph width
        /// </summary>
        public float WordSpacing { get; set; }


        /// <summary>
        /// Line spacing in units of max glyph width
        /// </summary>
        public float LineSpacing { get; set; }


        /// <summary>
        /// Whether to draw a drop-shadow. Note: this requires
        /// the QFont to have been loaded with a drop shadow to
        /// take effect.
        /// </summary>
        public bool DropShadowActive { get; set; }


        /// <summary>
        /// Offset of the shadow from the font glyphs in units of average glyph width
        /// </summary>
        public Vector2 DropShadowOffset { get; set; }


        /// <summary>
        /// Opacity of drop shadows
        /// </summary>
        public Color DropShadowColour { get; set; }


        /// <summary>
        /// Set the opacity of the drop shadow
        /// </summary>
        public float DropShadowOpacity { get; set; }
        
        public TFont()
        {
        }
    }
}
