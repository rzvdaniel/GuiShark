using System;
using System.Runtime.InteropServices;

namespace Gui.Shark.Core.Gfx
{
    /// <summary>
    /// Represents a color value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1, CharSet = CharSet.Unicode)]
    public struct TColor : IEquatable<TColor>, IComparable<TColor>
    {
        #region Basic colors

        /// <summary>
        /// The color #000000.
        /// </summary>
        public static readonly TColor Black = new TColor(0, 0, 0);

        /// <summary>
        /// The color #FFFFFF.
        /// </summary>
        public static readonly TColor White = new TColor(255, 255, 255);

        /// <summary>
        /// The color #FF0000.
        /// </summary>
        public static readonly TColor Red = new TColor(255, 0, 0);

        /// <summary>
        /// The color #FF00FF.
        /// </summary>
        public static readonly TColor Magenta = new TColor(255, 0, 255);

        /// <summary>
        /// The color #008000.
        /// </summary>
        public static readonly TColor Green = new TColor(0, 128, 0);

        /// <summary>
        /// The color #00FF00.
        /// </summary>
        public static readonly TColor PureGreen = new TColor(0, 255, 0);

        /// <summary>
        /// The color #0000FF.
        /// </summary>
        public static readonly TColor Blue = new TColor(0, 0, 255);

        /// <summary>
        /// The color #00000000.
        /// </summary>
        public static readonly TColor Transparent = new TColor(0, 0, 0, 0);

        #endregion

        #region Fields

        [FieldOffset(0)]
        readonly Byte _alpha;
        [FieldOffset(1)]
        readonly Byte _red;
        [FieldOffset(2)]
        readonly Byte _green;
        [FieldOffset(3)]
        readonly Byte _blue;
        [FieldOffset(0)]
        readonly Int32 _hashcode;

        #endregion

        #region ctor

        /// <summary>
        /// Creates a CSS color type without any transparency (alpha = 100%).
        /// </summary>
        /// <param name="r">The red value.</param>
        /// <param name="g">The green value.</param>
        /// <param name="b">The blue value.</param>
        public TColor(Byte r, Byte g, Byte b)
        {
            _hashcode = 0;
            _alpha = 255;
            _red = r;
            _blue = b;
            _green = g;
        }

        /// <summary>
        /// Creates a CSS color type.
        /// </summary>
        /// <param name="r">The red value.</param>
        /// <param name="g">The green value.</param>
        /// <param name="b">The blue value.</param>
        /// <param name="a">The alpha value.</param>
        public TColor(Byte r, Byte g, Byte b, Byte a)
        {
            _hashcode = 0;
            _alpha = a;
            _red = r;
            _blue = b;
            _green = g;
        }

        #endregion

        #region Static constructors

        /// <summary>
        /// Returns the color from the given primitives.
        /// </summary>
        /// <param name="r">The value for red [0,255].</param>
        /// <param name="g">The value for green [0,255].</param>
        /// <param name="b">The value for blue [0,255].</param>
        /// <param name="a">The value for alpha [0,1].</param>
        /// <returns>The CSS color value.</returns>
        public static TColor FromRgba(Byte r, Byte g, Byte b, Single a)
        {
            return new TColor(r, g, b, Normalize(a));
        }

        /// <summary>
        /// Returns the color from the given primitives.
        /// </summary>
        /// <param name="r">The value for red [0,1].</param>
        /// <param name="g">The value for green [0,1].</param>
        /// <param name="b">The value for blue [0,1].</param>
        /// <param name="a">The value for alpha [0,1].</param>
        /// <returns>The CSS color value.</returns>
        public static TColor FromRgba(Single r, Single g, Single b, Single a)
        {
            return new TColor(Normalize(r), Normalize(g), Normalize(b), Normalize(a));
        }

        /// <summary>
        /// Returns the gray color from the given value.
        /// </summary>
        /// <param name="number">The value for each component [0,255].</param>
        /// <param name="alpha">The value for alpha [0,1].</param>
        /// <returns>The CSS color value.</returns>
        public static TColor FromGray(Byte number, Single alpha = 1f)
        {
            return new TColor(number, number, number, Normalize(alpha));
        }

        /// <summary>
        /// Returns the gray color from the given value.
        /// </summary>
        /// <param name="value">The value for each component [0,1].</param>
        /// <param name="alpha">The value for alpha [0,1].</param>
        /// <returns>The CSS color value.</returns>
        public static TColor FromGray(Single value, Single alpha = 1f)
        {
            return FromGray(Normalize(value), alpha);
        }

        /// <summary>
        /// Returns the color from the given primitives without any alpha.
        /// </summary>
        /// <param name="r">The value for red [0,255].</param>
        /// <param name="g">The value for green [0,255].</param>
        /// <param name="b">The value for blue [0,255].</param>
        /// <returns>The CSS color value.</returns>
        public static TColor FromRgb(Byte r, Byte g, Byte b)
        {
            return new TColor(r, g, b);
        }

        /// <summary>
        /// Returns the color that represents the given HSL values.
        /// </summary>
        /// <param name="h">The color angle [0,1].</param>
        /// <param name="s">The saturation [0,1].</param>
        /// <param name="l">The light value [0,1].</param>
        /// <returns>The CSS color.</returns>
        public static TColor FromHsl(Single h, Single s, Single l)
        {
            return FromHsla(h, s, l, 1f);
        }

        /// <summary>
        /// Returns the color that represents the given HSL values.
        /// </summary>
        /// <param name="h">The color angle [0,1].</param>
        /// <param name="s">The saturation [0,1].</param>
        /// <param name="l">The light value [0,1].</param>
        /// <param name="alpha">The alpha value [0,1].</param>
        /// <returns>The CSS color.</returns>
        public static TColor FromHsla(Single h, Single s, Single l, Single alpha)
        {
            const Single third = 1f / 3f;

            var m2 = l <= 0.5f ? (l * (s + 1f)) : (l + s - l * s);
            var m1 = 2f * l - m2;
            var r = Convert(HueToRgb(m1, m2, h + third));
            var g = Convert(HueToRgb(m1, m2, h));
            var b = Convert(HueToRgb(m1, m2, h - third));
            return new TColor(r, g, b, Normalize(alpha));
        }

        /// <summary>
        /// Returns the color that represents Hue-Whiteness-Blackness.
        /// </summary>
        /// <param name="h">The color angle [0,1].</param>
        /// <param name="w">The whiteness [0,1].</param>
        /// <param name="b">The blackness [0,1].</param>
        /// <returns>The CSS color.</returns>
        public static TColor FromHwb(Single h, Single w, Single b)
        {
            return FromHwba(h, w, b, 1f);
        }

        /// <summary>
        /// Returns the color that represents Hue-Whiteness-Blackness.
        /// </summary>
        /// <param name="h">The color angle [0,1].</param>
        /// <param name="w">The whiteness [0,1].</param>
        /// <param name="b">The blackness [0,1].</param>
        /// <param name="alpha">The alpha value [0,1].</param>
        /// <returns>The CSS color.</returns>
        public static TColor FromHwba(Single h, Single w, Single b, Single alpha)
        {
            var ratio = 1f / (w + b);
            var red = 0f;
            var green = 0f;
            var blue = 0f;

            if (ratio < 1f)
            {
                w *= ratio;
                b *= ratio;
            }

            var p = (Int32)(6 * h);
            var f = 6 * h - p;

            if ((p & 0x01) != 0)
                f = 1 - f;

            var v = 1 - b;
            var n = w + f * (v - w);

            switch (p)
            {
                default:
                case 6:
                case 0: red = v; green = n; blue = w; break;
                case 1: red = n; green = v; blue = w; break;
                case 2: red = w; green = v; blue = n; break;
                case 3: red = w; green = n; blue = v; break;
                case 4: red = n; green = w; blue = v; break;
                case 5: red = v; green = w; blue = n; break;
            }

            return FromRgba(red, green, blue, alpha);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Int32 value of the color.
        /// </summary>
        public Int32 Value
        {
            get { return _hashcode; }
        }

        /// <summary>
        /// Gets the alpha part of the color.
        /// </summary>
        public Byte A
        {
            get { return _alpha; }
        }

        /// <summary>
        /// Gets the alpha part of the color in percent (0..1).
        /// </summary>
        public Double Alpha
        {
            get { return _alpha / 255.0; }
        }

        /// <summary>
        /// Gets the red part of the color.
        /// </summary>
        public Byte R
        {
            get { return _red; }
        }

        /// <summary>
        /// Gets the green part of the color.
        /// </summary>
        public Byte G
        {
            get { return _green; }
        }

        /// <summary>
        /// Gets the blue part of the color.
        /// </summary>
        public Byte B
        {
            get { return _blue; }
        }

        #endregion

        #region Equality

        /// <summary>
        /// Compares two colors and returns a boolean indicating if the two do match.
        /// </summary>
        /// <param name="a">The first color to use.</param>
        /// <param name="b">The second color to use.</param>
        /// <returns>True if both colors are equal, otherwise false.</returns>
        public static Boolean operator ==(TColor a, TColor b)
        {
            return a._hashcode == b._hashcode;
        }

        /// <summary>
        /// Compares two colors and returns a boolean indicating if the two do not match.
        /// </summary>
        /// <param name="a">The first color to use.</param>
        /// <param name="b">The second color to use.</param>
        /// <returns>True if both colors are not equal, otherwise false.</returns>
        public static Boolean operator !=(TColor a, TColor b)
        {
            return a._hashcode != b._hashcode;
        }

        /// <summary>
        /// Checks two colors for equality.
        /// </summary>
        /// <param name="other">The other color.</param>
        /// <returns>True if both colors or equal, otherwise false.</returns>
        public Boolean Equals(TColor other)
        {
            return this._hashcode == other._hashcode;
        }

        /// <summary>
        /// Tests if another object is equal to this object.
        /// </summary>
        /// <param name="obj">The object to test with.</param>
        /// <returns>True if the two objects are equal, otherwise false.</returns>
        public override Boolean Equals(Object obj)
        {
            var other = obj as TColor?;

            if (other != null)
            {
                return Equals(other.Value);
            }

            return false;
        }

        Int32 IComparable<TColor>.CompareTo(TColor other)
        {
            return _hashcode - other._hashcode;
        }

        /// <summary>
        /// Returns a hash code that defines the current color.
        /// </summary>
        /// <returns>The integer value of the hashcode.</returns>
        public override Int32 GetHashCode()
        {
            return _hashcode;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Mixes two colors using alpha compositing as described here:
        /// http://en.wikipedia.org/wiki/Alpha_compositing
        /// </summary>
        /// <param name="above">The first color (above) with transparency.</param>
        /// <param name="below">The second color (below the first one) without transparency.</param>
        /// <returns>The outcome in the crossing section.</returns>
        public static TColor Mix(TColor above, TColor below)
        {
            return Mix(above.Alpha, above, below);
        }

        /// <summary>
        /// Mixes two colors using alpha compositing as described here:
        /// http://en.wikipedia.org/wiki/Alpha_compositing
        /// </summary>
        /// <param name="alpha">The mixing parameter.</param>
        /// <param name="above">The first color (above) (no transparency).</param>
        /// <param name="below">The second color (below the first one) (no transparency).</param>
        /// <returns>The outcome in the crossing section.</returns>
        public static TColor Mix(Double alpha, TColor above, TColor below)
        {
            var gamma = 1.0 - alpha;
            var r = gamma * below.R + alpha * above.R;
            var g = gamma * below.G + alpha * above.G;
            var b = gamma * below.B + alpha * above.B;
            return new TColor((Byte)r, (Byte)g, (Byte)b);
        }

        #endregion

        #region Helpers

        static Byte Normalize(Single value)
        {
            return (Byte)Math.Max(Math.Min(Math.Round(255 * value), 255), 0);
        }

        static Byte Convert(Single value)
        {
            return (Byte)Math.Round(255f * value);
        }

        static Single HueToRgb(Single m1, Single m2, Single h)
        {
            const Single oneSixth = 1f / 6f;
            const Single twoThird = 2f / 3f;

            if (h < 0f)
                h += 1f;
            else if (h > 1f)
                h -= 1f;

            if (h < oneSixth)
                return m1 + (m2 - m1) * h * 6f;
            else if (h < 0.5)
                return m2;
            else if (h < twoThird)
                return m1 + (m2 - m1) * (twoThird - h) * 6f;

            return m1;
        }

        #endregion
    }
}
