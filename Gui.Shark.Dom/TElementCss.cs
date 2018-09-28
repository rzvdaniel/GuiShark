using AngleSharp.Attributes;
using AngleSharp.Css.Values;
using System.ComponentModel;

namespace Gui.Shark.Dom
{
    /// <summary>
    /// Represents a single CSS declaration block.
    /// </summary>
    [DomName("ElementCss")]
    public class TElementCss : INotifyPropertyChanged
    {
        #region CSS Property Values

        /// <summary>
        /// Gets or sets the accelerator value.
        /// </summary>
        [DomName("accelerator")]
        public string Accelerator { get; set; }

        /// <summary>
        /// Gets or sets the align-content value.
        /// </summary>
        [DomName("alignContent")]
        public string AlignContent { get; set; }

        /// <summary>
        /// Gets or sets the align-items value.
        /// </summary>
        [DomName("alignItems")]
        public string AlignItems { get; set; }

        /// <summary>
        /// Gets or sets the alignment-baseline value.
        /// </summary>
        [DomName("alignmentBaseline")]
        public string AlignmentBaseline { get; set; }

        /// <summary>
        /// Gets or sets the align-self value.
        /// </summary>
        [DomName("alignSelf")]
        public string AlignSelf { get; set; }

        /// <summary>
        /// Gets or sets the animation value.
        /// </summary>
        [DomName("animation")]
        public string Animation { get; set; }

        /// <summary>
        /// Gets or sets the animation-delay value.
        /// </summary>
        [DomName("animationDelay")]
        public string AnimationDelay { get; set; }

        /// <summary>
        /// Gets or sets the animation-direction value.
        /// </summary>
        [DomName("animationDirection")]
        public string AnimationDirection { get; set; }

        /// <summary>
        /// Gets or sets the animation-duration value.
        /// </summary>
        [DomName("animationDuration")]
        public string AnimationDuration { get; set; }

        /// <summary>
        /// Gets or sets the animation-fill-mode value.
        /// </summary>
        [DomName("animationFillMode")]
        public string AnimationFillMode { get; set; }

        /// <summary>
        /// Gets or sets the animation-iteration-count value.
        /// </summary>
        [DomName("animationIterationCount")]
        public string AnimationIterationCount { get; set; }

        /// <summary>
        /// Gets or sets the animation-name value.
        /// </summary>
        [DomName("animationName")]
        public string AnimationName { get; set; }

        /// <summary>
        /// Gets or sets the animation-play-state value.
        /// </summary>
        [DomName("animationPlayState")]
        public string AnimationPlayState { get; set; }

        /// <summary>
        /// Gets or sets the animation-timing-function value.
        /// </summary>
        [DomName("animationTimingFunction")]
        public string AnimationTimingFunction { get; set; }

        /// <summary>
        /// Gets or sets the backface-visibility value.
        /// </summary>
        [DomName("backfaceVisibility")]
        public string BackfaceVisibility { get; set; }

        /// <summary>
        /// Gets or sets the background value.
        /// </summary>
        [DomName("background")]
        public string Background { get; set; }

        /// <summary>
        /// Gets or sets the background-attachment value.
        /// </summary>
        [DomName("backgroundAttachment")]
        public string BackgroundAttachment { get; set; }

        /// <summary>
        /// Gets or sets the background-clip value.
        /// </summary>
        [DomName("backgroundClip")]
        public string BackgroundClip { get; set; }

        /// <summary>
        /// Gets or sets the background-color value.
        /// </summary>
        [DomName("backgroundColor")]
        public Color BackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the background-image value.
        /// </summary>
        [DomName("backgroundImage")]
        public string BackgroundImage { get; set; }

        /// <summary>
        /// Gets or sets the background-origin value.
        /// </summary>
        [DomName("backgroundOrigin")]
        public string BackgroundOrigin { get; set; }

        /// <summary>
        /// Gets or sets the background-position value.
        /// </summary>
        [DomName("backgroundPosition")]
        public string BackgroundPosition { get; set; }

        /// <summary>
        /// Gets or sets the background-position-x value.
        /// </summary>
        [DomName("backgroundPositionX")]
        public string BackgroundPositionX { get; set; }

        /// <summary>
        /// Gets or sets the background-position-y value.
        /// </summary>
        [DomName("backgroundPositionY")]
        public string BackgroundPositionY { get; set; }

        /// <summary>
        /// Gets or sets the background-repeat value.
        /// </summary>
        [DomName("backgroundRepeat")]
        public string BackgroundRepeat { get; set; }

        /// <summary>
        /// Gets or sets the background-size value.
        /// </summary>
        [DomName("backgroundSize")]
        public string BackgroundSize { get; set; }

        /// <summary>
        /// Gets or sets the baseline-shift value.
        /// </summary>
        [DomName("baselineShift")]
        public string BaselineShift { get; set; }

        /// <summary>
        /// Gets or sets the behavior value.
        /// </summary>
        [DomName("behavior")]
        public string Behavior { get; set; }

        /// <summary>
        /// Gets or sets the border value.
        /// </summary>
        [DomName("border")]
        public string Border { get; set; }

        /// <summary>
        /// Gets or sets the bottom value.
        /// </summary>
        [DomName("bottom")]
        public string Bottom { get; set; }

        /// <summary>
        /// Gets or sets the border-bottom value.
        /// </summary>
        [DomName("borderBottom")]
        public string BorderBottom { get; set; }

        /// <summary>
        /// Gets or sets the border-bottom-color value.
        /// </summary>
        [DomName("borderBottomColor")]
        public string BorderBottomColor { get; set; }

        /// <summary>
        /// Gets or sets the border-bottom-left-radius value.
        /// </summary>
        [DomName("borderBottomLeftRadius")]
        public string BorderBottomLeftRadius { get; set; }

        /// <summary>
        /// Gets or sets the border-bottom-right-radius value.
        /// </summary>
        [DomName("borderBottomRightRadius")]
        public string BorderBottomRightRadius { get; set; }

        /// <summary>
        /// Gets or sets the border-bottom-style value.
        /// </summary>
        [DomName("borderBottomStyle")]
        public string BorderBottomStyle { get; set; }

        /// <summary>
        /// Gets or sets the border-bottom-width value.
        /// </summary>
        [DomName("borderBottomWidth")]
        public string BorderBottomWidth { get; set; }

        /// <summary>
        /// Gets or sets the border-collapse value.
        /// </summary>
        [DomName("borderCollapse")]
        public string BorderCollapse { get; set; }

        /// <summary>
        /// Gets or sets the border-color value.
        /// </summary>
        [DomName("borderColor")]
        public string BorderColor { get; set; }

        /// <summary>
        /// Gets or sets the border-image value.
        /// </summary>
        [DomName("borderImage")]
        public string BorderImage { get; set; }

        /// <summary>
        /// Gets or sets the border-image-outset value.
        /// </summary>
        [DomName("borderImageOutset")]
        public string BorderImageOutset { get; set; }

        /// <summary>
        /// Gets or sets the border-image-repeat value.
        /// </summary>
        [DomName("borderImageRepeat")]
        public string BorderImageRepeat { get; set; }

        /// <summary>
        /// Gets or sets the border-image-slice value.
        /// </summary>
        [DomName("borderImageSlice")]
        public string BorderImageSlice { get; set; }

        /// <summary>
        /// Gets or sets the border-image-source value.
        /// </summary>
        [DomName("borderImageSource")]
        public string BorderImageSource { get; set; }

        /// <summary>
        /// Gets or sets the border-image-width value.
        /// </summary>
        [DomName("borderImageWidth")]
        public string BorderImageWidth { get; set; }

        /// <summary>
        /// Gets or sets the border-left value.
        /// </summary>
        [DomName("borderLeft")]
        public string BorderLeft { get; set; }

        /// <summary>
        /// Gets or sets the border-left-color value.
        /// </summary>
        [DomName("borderLeftColor")]
        public string BorderLeftColor { get; set; }

        /// <summary>
        /// Gets or sets the border-left-style value.
        /// </summary>
        [DomName("borderLeftStyle")]
        public string BorderLeftStyle { get; set; }

        /// <summary>
        /// Gets or sets the border-left-width value.
        /// </summary>
        [DomName("borderLeftWidth")]
        public string BorderLeftWidth { get; set; }

        /// <summary>
        /// Gets or sets the border-radius value.
        /// </summary>
        [DomName("borderRadius")]
        public string BorderRadius { get; set; }

        /// <summary>
        /// Gets or sets the border-right value.
        /// </summary>
        [DomName("borderRight")]
        public string BorderRight { get; set; }

        /// <summary>
        /// Gets or sets the border-right-color value.
        /// </summary>
        [DomName("borderRightColor")]
        public string BorderRightColor { get; set; }

        /// <summary>
        /// Gets or sets the border-right-style value.
        /// </summary>
        [DomName("borderRightStyle")]
        public string BorderRightStyle { get; set; }

        /// <summary>
        /// Gets or sets the border-right-width value.
        /// </summary>
        [DomName("borderRightWidth")]
        public string BorderRightWidth { get; set; }

        /// <summary>
        /// Gets or sets the border-spacing value.
        /// </summary>
        [DomName("borderSpacing")]
        public string BorderSpacing { get; set; }

        /// <summary>
        /// Gets or sets the border-style value.
        /// </summary>
        [DomName("borderStyle")]
        public string BorderStyle { get; set; }

        /// <summary>
        /// Gets or sets the border-top value.
        /// </summary>
        [DomName("borderTop")]
        public string BorderTop { get; set; }

        /// <summary>
        /// Gets or sets the border-top-color value.
        /// </summary>
        [DomName("borderTopColor")]
        public string BorderTopColor { get; set; }

        /// <summary>
        /// Gets or sets the border-top-left-radius value.
        /// </summary>
        [DomName("borderTopLeftRadius")]
        public string BorderTopLeftRadius { get; set; }

        /// <summary>
        /// Gets or sets the border-top-right-radius value.
        /// </summary>
        [DomName("borderTopRightRadius")]
        public string BorderTopRightRadius { get; set; }

        /// <summary>
        /// Gets or sets the border-top-style value.
        /// </summary>
        [DomName("borderTopStyle")]
        public string BorderTopStyle { get; set; }

        /// <summary>
        /// Gets or sets the border-top-width value.
        /// </summary>
        [DomName("borderTopWidth")]
        public string BorderTopWidth { get; set; }

        /// <summary>
        /// Gets or sets the border-width value.
        /// </summary>
        [DomName("borderWidth")]
        public string BorderWidth { get; set; }

        /// <summary>
        /// Gets or sets the box-shadow value.
        /// </summary>
        [DomName("boxShadow")]
        public string BoxShadow { get; set; }

        /// <summary>
        /// Gets or sets the box-sizing value.
        /// </summary>
        [DomName("boxSizing")]
        public string BoxSizing { get; set; }

        /// <summary>
        /// Gets or sets the break-after value.
        /// </summary>
        [DomName("breakAfter")]
        public string BreakAfter { get; set; }

        /// <summary>
        /// Gets or sets the break-before value.
        /// </summary>
        [DomName("breakBefore")]
        public string BreakBefore { get; set; }

        /// <summary>
        /// Gets or sets the break-inside value.
        /// </summary>
        [DomName("breakInside")]
        public string BreakInside { get; set; }

        /// <summary>
        /// Gets or sets the caption-side value.
        /// </summary>
        [DomName("captionSide")]
        public string CaptionSide { get; set; }

        /// <summary>
        /// Gets or sets the clear value.
        /// </summary>
        [DomName("clear")]
        public string Clear { get; set; }

        /// <summary>
        /// Gets or sets the clip value.
        /// </summary>
        [DomName("clip")]
        public string Clip { get; set; }

        /// <summary>
        /// Gets or sets the clip-bottom value.
        /// </summary>
        [DomName("clipBottom")]
        public string ClipBottom { get; set; }

        /// <summary>
        /// Gets or sets the clip-left value.
        /// </summary>
        [DomName("clipLeft")]
        public string ClipLeft { get; set; }

        /// <summary>
        /// Gets or sets the clip-path value.
        /// </summary>
        [DomName("clipPath")]
        public string ClipPath { get; set; }

        /// <summary>
        /// Gets or sets the clip-right value.
        /// </summary>
        [DomName("clipRight")]
        public string ClipRight { get; set; }

        /// <summary>
        /// Gets or sets the clip-rule value.
        /// </summary>
        [DomName("clipRule")]
        public string ClipRule { get; set; }

        /// <summary>
        /// Gets or sets the clip-top value.
        /// </summary>
        [DomName("clipTop")]
        public string ClipTop { get; set; }

        /// <summary>
        /// Gets or sets the color value.
        /// </summary>
        [DomName("color")]
        public Color Color { get; set; }

        /// <summary>
        /// Gets or sets the color-interpolation-filters value.
        /// </summary>
        [DomName("colorInterpolationFilters")]
        public string ColorInterpolationFilters { get; set; }

        /// <summary>
        /// Gets or sets the column-count value.
        /// </summary>
        [DomName("columnCount")]
        public string ColumnCount { get; set; }

        /// <summary>
        /// Gets or sets the column-fill value.
        /// </summary>
        [DomName("columnFill")]
        public string ColumnFill { get; set; }

        /// <summary>
        /// Gets or sets the column-gap value.
        /// </summary>
        [DomName("columnGap")]
        public string ColumnGap { get; set; }

        /// <summary>
        /// Gets or sets the column-rule value.
        /// </summary>
        [DomName("columnRule")]
        public string ColumnRule { get; set; }

        /// <summary>
        /// Gets or sets the column-rule-color value.
        /// </summary>
        [DomName("columnRuleColor")]
        public string ColumnRuleColor { get; set; }

        /// <summary>
        /// Gets or sets the column-rule-style value.
        /// </summary>
        [DomName("columnRuleStyle")]
        public string ColumnRuleStyle { get; set; }

        /// <summary>
        /// Gets or sets the column-rule-width value.
        /// </summary>
        [DomName("columnRuleWidth")]
        public string ColumnRuleWidth { get; set; }

        /// <summary>
        /// Gets or sets the columnsv
        /// </summary>
        [DomName("columns")]
        public string Columns { get; set; }

        /// <summary>
        /// Gets or sets the column-span value.
        /// </summary>
        [DomName("columnSpan")]
        public string ColumnSpan { get; set; }

        /// <summary>
        /// Gets or sets the column-width value.
        /// </summary>
        [DomName("columnWidth")]
        public string ColumnWidth { get; set; }

        /// <summary>
        /// Gets or sets the content value.
        /// </summary>
        [DomName("content")]
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the counter-increment value.
        /// </summary>
        [DomName("counterIncrement")]
        public string CounterIncrement { get; set; }
        /// <summary>
        /// Gets or sets the counter-reset value.
        /// </summary>
        [DomName("counterReset")]
        public string CounterReset { get; set; }

        /// <summary>
        /// Gets or sets the cursor value.
        /// </summary>
        [DomName("cursor")]
        public string Cursor { get; set; }

        /// <summary>
        /// Gets or sets the direction value.
        /// </summary>
        [DomName("direction")]
        public string Direction { get; set; }

        /// <summary>
        /// Gets or sets the display value.
        /// </summary>
        [DomName("display")]
        public string Display { get; set; }

        /// <summary>
        /// Gets or sets the dominant-baseline value.
        /// </summary>
        [DomName("dominantBaseline")]
        public string DominantBaseline { get; set; }

        /// <summary>
        /// Gets or sets the empty-cells value.
        /// </summary>
        [DomName("emptyCells")]
        public string EmptyCells { get; set; }

        /// <summary>
        /// Gets or sets the enable-background value.
        /// </summary>
        [DomName("enableBackground")]
        public string EnableBackground { get; set; }

        /// <summary>
        /// Gets or sets the fill value.
        /// </summary>
        [DomName("fill")]
        public string Fill { get; set; }

        /// <summary>
        /// Gets or sets the fill-opacity value.
        /// </summary>
        [DomName("fillOpacity")]
        public string FillOpacity { get; set; }

        /// <summary>
        /// Gets or sets the fill-rule value.
        /// </summary>
        [DomName("fillRule")]
        public string FillRule { get; set; }

        /// <summary>
        /// Gets or sets the filter value.
        /// </summary>
        [DomName("filter")]
        public string Filter { get; set; }

        /// <summary>
        /// Gets or sets the flex value.
        /// </summary>
        [DomName("flex")]
        public string Flex { get; set; }

        /// <summary>
        /// Gets or sets the flex-basis value.
        /// </summary>
        [DomName("flexBasis")]
        public string FlexBasis { get; set; }

        /// <summary>
        /// Gets or sets the flex-direction value.
        /// </summary>
        [DomName("flexDirection")]
        public string FlexDirection { get; set; }

        /// <summary>
        /// Gets or sets the flex-flow value.
        /// </summary>
        [DomName("flexFlow")]
        public string FlexFlow { get; set; }

        /// <summary>
        /// Gets or sets the flex-grow value.
        /// </summary>
        [DomName("flexGrow")]
        public string FlexGrow { get; set; }

        /// <summary>
        /// Gets or sets the flex-shrink value. 
        /// </summary>
        [DomName("flexShrink")]
        public string FlexShrink { get; set; }

        /// <summary>
        /// Gets or sets the flex-wrap value.
        /// </summary>
        [DomName("flexWrap")]
        public string FlexWrap { get; set; }

        /// <summary>
        /// Gets or sets the float value.
        /// </summary>
        [DomName("cssFloat")]
        public string Float { get; set; }

        /// <summary>
        /// Gets or sets the font value.
        /// </summary>
        [DomName("font")]
        public string Font { get; set; }

        /// <summary>
        /// Gets or sets the font-family value.
        /// </summary>
        [DomName("fontFamily")]
        public string FontFamily { get; set; }

        /// <summary>
        /// Gets or sets the font-feature-settings value.
        /// </summary>
        [DomName("fontFeatureSettings")]
        public string FontFeatureSettings { get; set; }

        /// <summary>
        /// Gets or sets the font-size value.
        /// </summary>
        [DomName("fontSize")]
        public string FontSize { get; set; }

        /// <summary>
        /// Gets or sets the font-size-adjust value.
        /// </summary>
        [DomName("fontSizeAdjust")]
        public string FontSizeAdjust { get; set; }

        /// <summary>
        /// Gets or sets the font-stretch value.
        /// </summary>
        [DomName("fontStretch")]
        public string FontStretch { get; set; }

        /// <summary>
        /// Gets or sets the font-style value.
        /// </summary>
        [DomName("fontStyle")]
        public string FontStyle { get; set; }

        /// <summary>
        /// Gets or sets the font-variant value.
        /// </summary>
        [DomName("fontVariant")]
        public string FontVariant { get; set; }

        /// <summary>
        /// Gets or sets the font-weight value.
        /// </summary>
        [DomName("fontWeight")]
        public string FontWeight { get; set; }

        /// <summary>
        /// Gets or sets the glyph-orientation-horizontal value.
        /// </summary>
        [DomName("glyphOrientationHorizontal")]
        public string GlyphOrientationHorizontal { get; set; }

        /// <summary>
        /// Gets or sets the glyph-orientation-vertical value.
        /// </summary>
        [DomName("glyphOrientationVertical")]
        public string GlyphOrientationVertical { get; set; }

        /// <summary>
        /// Gets or sets the height value.
        /// </summary>
        [DomName("height")]
        public Length Height { get; set; }

        /// <summary>
        /// Gets or sets the ime-mode value.
        /// </summary>
        [DomName("imeMode")]
        public string ImeMode { get; set; }

        /// <summary>
        /// Gets or sets the justify-content value.
        /// </summary>
        [DomName("justifyContent")]
        public string JustifyContent { get; set; }

        /// <summary>
        /// Gets or sets the layout-grid value.
        /// </summary>
        [DomName("layoutGrid")]
        public string LayoutGrid { get; set; }

        /// <summary>
        /// Gets or sets the layout-grid-char value.
        /// </summary>
        [DomName("layoutGridChar")]
        public string LayoutGridChar { get; set; }

        /// <summary>
        /// Gets or sets the layout-grid-line value.
        /// </summary>
        [DomName("layoutGridLine")]
        public string LayoutGridLine { get; set; }

        /// <summary>
        /// Gets or sets the layout-grid-mode value.
        /// </summary>
        [DomName("layoutGridMode")]
        public string LayoutGridMode { get; set; }

        /// <summary>
        /// Gets or sets the layout-grid-type value.
        /// </summary>
        [DomName("layoutGridType")]
        public string LayoutGridType { get; set; }

        /// <summary>
        /// Gets or sets the left value.
        /// </summary>
        [DomName("left")]
        public string Left { get; set; }

        /// <summary>
        /// Gets or sets the letter-spacing value.
        /// </summary>
        [DomName("letterSpacing")]
        public string LetterSpacing { get; set; }

        /// <summary>
        /// Gets or sets the line-height value.
        /// </summary>
        [DomName("lineHeight")]
        public string LineHeight { get; set; }

        /// <summary>
        /// Gets or sets the list-style value.
        /// </summary>
        [DomName("listStyle")]
        public string ListStyle { get; set; }

        /// <summary>
        /// Gets or sets the list-style-image value.
        /// </summary>
        [DomName("listStyleImage")]
        public string ListStyleImage { get; set; }

        /// <summary>
        /// Gets or sets the list-style-position value.
        /// </summary>
        [DomName("listStylePosition")]
        public string ListStylePosition { get; set; }

        /// <summary>
        /// Gets or sets the list-style-type value.
        /// </summary>
        [DomName("listStyleType")]
        public string ListStyleType { get; set; }

        /// <summary>
        /// Gets or sets the margin value.
        /// </summary>
        [DomName("margin")]
        public Length Margin { get; set; }

        /// <summary>
        /// Gets or sets the margin-bottom value.
        /// </summary>
        [DomName("marginBottom")]
        public Length MarginBottom { get; set; }

        /// <summary>
        /// Gets or sets the margin-left value.
        /// </summary>
        [DomName("marginLeft")]
        public Length MarginLeft { get; set; }

        /// <summary>
        /// Gets or sets the margin-right value.
        /// </summary>
        [DomName("marginRight")]
        public Length MarginRight { get; set; }

        /// <summary>
        /// Gets or sets the margin-top value.
        /// </summary>
        [DomName("marginTop")]
        public Length MarginTop { get; set; }

        /// <summary>
        /// Gets or sets the marker value.
        /// </summary>
        [DomName("marker")]
        public string Marker { get; set; }

        /// <summary>
        /// Gets or sets the marker-end value.
        /// </summary>
        [DomName("markerEnd")]
        public string MarkerEnd { get; set; }

        /// <summary>
        /// Gets or sets the marker-mid value.
        /// </summary>
        [DomName("markerMid")]
        public string MarkerMid { get; set; }

        /// <summary>
        /// Gets or sets the marker-start value.
        /// </summary>
        [DomName("markerStart")]
        public string MarkerStart { get; set; }

        /// <summary>
        /// Gets or sets the mask value.
        /// </summary>
        [DomName("mask")]
        public string Mask { get; set; }

        /// <summary>
        /// Gets or sets the max-height value.
        /// </summary>
        [DomName("maxHeight")]
        public Length MaxHeight { get; set; }

        /// <summary>
        /// Gets or sets the max-width value.
        /// </summary>
        [DomName("maxWidth")]
        public Length MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the min-height value.
        /// </summary>
        [DomName("minHeight")]
        public Length MinHeight { get; set; }

        /// <summary>
        /// Gets or sets the min-width value.
        /// </summary>
        [DomName("minWidth")]
        public Length MinWidth { get; set; }

        /// <summary>
        /// Gets or sets the opacity value.
        /// </summary>
        [DomName("opacity")]
        public string Opacity { get; set; }

        /// <summary>
        /// Gets or sets the order value.
        /// </summary>
        [DomName("order")]
        public string Order { get; set; }

        /// <summary>
        /// Gets or sets the orphans value.
        /// </summary>
        [DomName("orphans")]
        public string Orphans { get; set; }

        /// <summary>
        /// Gets or sets the outline value.
        /// </summary>
        [DomName("outline")]
        public string Outline { get; set; }

        /// <summary>
        /// Gets or sets the outline-color value.
        /// </summary>
        [DomName("outlineColor")]
        public string OutlineColor { get; set; }

        /// <summary>
        /// Gets or sets the outline-style value.
        /// </summary>
        [DomName("outlineStyle")]
        public string OutlineStyle { get; set; }

        /// <summary>
        /// Gets or sets the outline-width value.
        /// </summary>
        [DomName("outlineWidth")]
        public string OutlineWidth { get; set; }

        /// <summary>
        /// Gets or sets the overflow value.
        /// </summary>
        [DomName("overflow")]
        public string Overflow { get; set; }

        /// <summary>
        /// Gets or sets the overflow-x value.
        /// </summary>
        [DomName("overflowX")]
        public string OverflowX { get; set; }

        /// <summary>
        /// Gets or sets the overflow-y value.
        /// </summary>
        [DomName("overflowY")]
        public string OverflowY { get; set; }

        /// <summary>
        /// Gets or sets the overflow-wrap value.
        /// </summary>
        [DomName("overflowWrap")]
        public string OverflowWrap { get; set; }

        /// <summary>
        /// Gets or sets the padding value.
        /// </summary>
        [DomName("padding")]
        public Length Padding { get; set; }

        /// <summary>
        /// Gets or sets the padding-bottom value.
        /// </summary>
        [DomName("paddingBottom")]
        public Length PaddingBottom { get; set; }

        /// <summary>
        /// Gets or sets the padding-left value.
        /// </summary>
        [DomName("paddingLeft")]
        public Length PaddingLeft { get; set; }

        /// <summary>
        /// Gets or sets the padding-right value.
        /// </summary>
        [DomName("paddingRight")]
        public Length PaddingRight { get; set; }

        /// <summary>
        /// Gets or sets the padding-top value.
        /// </summary>
        [DomName("paddingTop")]
        public Length PaddingTop { get; set; }

        /// <summary>
        /// Gets or sets the page-break-after value.
        /// </summary>
        [DomName("pageBreakAfter")]
        public string PageBreakAfter { get; set; }

        /// <summary>
        /// Gets or sets the page-break-before value.
        /// </summary>
        [DomName("pageBreakBefore")]
        public string PageBreakBefore { get; set; }

        /// <summary>
        /// Gets or sets the page-break-inside value.
        /// </summary>
        [DomName("pageBreakInside")]
        public string PageBreakInside { get; set; }

        /// <summary>
        /// Gets or sets the perspective value.
        /// </summary>
        [DomName("perspective")]
        public string Perspective { get; set; }

        /// <summary>
        /// Gets or sets the perspective-origin value.
        /// </summary>
        [DomName("perspectiveOrigin")]
        public string PerspectiveOrigin { get; set; }

        /// <summary>
        /// Gets or sets the pointer-events value.
        /// </summary>
        [DomName("pointerEvents")]
        public string PointerEvents { get; set; }

        /// <summary>
        /// Gets or sets the position value.
        /// </summary>
        [DomName("position")]
        public string Position { get; set; }

        /// <summary>
        /// Gets or sets the quotes value.
        /// </summary>
        [DomName("quotes")]
        public string Quotes { get; set; }

        /// <summary>
        /// Gets or sets the right value.
        /// </summary>
        [DomName("right")]
        public string Right { get; set; }

        /// <summary>
        /// Gets or sets the ruby-align value.
        /// </summary>
        [DomName("rubyAlign")]
        public string RubyAlign { get; set; }

        /// <summary>
        /// Gets or sets the ruby-overhang value.
        /// </summary>
        [DomName("rubyOverhang")]
        public string RubyOverhang { get; set; }

        /// <summary>
        /// Gets or sets the ruby-position value.
        /// </summary>
        [DomName("rubyPosition")]
        public string RubyPosition { get; set; }

        /// <summary>
        /// Gets or sets the scrollbar3d-light-color value.
        /// </summary>
        [DomName("scrollbar3dLightColor")]
        public string Scrollbar3dLightColor { get; set; }

        /// <summary>
        /// Gets or sets the scrollbar-arrow-color value.
        /// </summary>
        [DomName("scrollbarArrowColor")]
        public string ScrollbarArrowColor { get; set; }

        /// <summary>
        /// Gets or sets the scrollbar-dark-shadow-color value.
        /// </summary>
        [DomName("scrollbarDarkShadowColor")]
        public string ScrollbarDarkShadowColor { get; set; }

        /// <summary>
        /// Gets or sets the scrollbar-face-color value.
        /// </summary>
        [DomName("scrollbarFaceColor")]
        public string ScrollbarFaceColor { get; set; }

        /// <summary>
        /// Gets or sets the scrollbar-highlight-color value.
        /// </summary>
        [DomName("scrollbarHighlightColor")]
        public string ScrollbarHighlightColor { get; set; }

        /// <summary>
        /// Gets or sets the scrollbar-shadow-color value.
        /// </summary>
        [DomName("scrollbarShadowColor")]
        public string ScrollbarShadowColor { get; set; }

        /// <summary>
        /// Gets or sets the scrollbar-track-color value.
        /// </summary>
        [DomName("scrollbarTrackColor")]
        public string ScrollbarTrackColor { get; set; }

        /// <summary>
        /// Gets or sets the stroke value.
        /// </summary>
        [DomName("stroke")]
        public string Stroke { get; set; }

        /// <summary>
        /// Gets or sets the stroke-dasharray value.
        /// </summary>
        [DomName("strokeDasharray")]
        public string StrokeDasharray { get; set; }

        /// <summary>
        /// Gets or sets the stroke-dashoffset value.
        /// </summary>
        [DomName("strokeDashoffset")]
        public string StrokeDashoffset { get; set; }

        /// <summary>
        /// Gets or sets the stroke-linecap value.
        /// </summary>
        [DomName("strokeLinecap")]
        public string StrokeLinecap { get; set; }

        /// <summary>
        /// Gets or sets the stroke-line-join value.
        /// </summary>
        [DomName("strokeLinejoin")]
        public string StrokeLinejoin { get; set; }

        /// <summary>
        /// Gets or sets the stroke-miterlimit value.
        /// </summary>
        [DomName("strokeMiterlimit")]
        public string StrokeMiterlimit { get; set; }

        /// <summary>
        /// Gets or sets the stroke-opacity value.
        /// </summary>
        [DomName("strokeOpacity")]
        public string StrokeOpacity { get; set; }

        /// <summary>
        /// Gets or sets the stroke-width value.
        /// </summary>
        [DomName("strokeWidth")]
        public string StrokeWidth { get; set; }
        /// <summary>
        /// Gets or sets the table-layout value.
        /// </summary>
        [DomName("tableLayout")]
        public string TableLayout { get; set; }

        /// <summary>
        /// Gets or sets the text-align value.
        /// </summary>
        [DomName("textAlign")]
        public string TextAlign { get; set; }

        /// <summary>
        /// Gets or sets the text-align-last value.
        /// </summary>
        [DomName("textAlignLast")]
        public string TextAlignLast { get; set; }

        /// <summary>
        /// Gets or sets the text-anchor value.
        /// </summary>
        [DomName("textAnchor")]
        public string TextAnchor { get; set; }

        /// <summary>
        /// Gets or sets the text-autospace value.
        /// </summary>
        [DomName("textAutospace")]
        public string TextAutospace { get; set; }

        /// <summary>
        /// Gets or sets the text-decoration value.
        /// </summary>
        [DomName("textDecoration")]
        public string TextDecoration { get; set; }

        /// <summary>
        /// Gets or sets the text-indent value.
        /// </summary>
        [DomName("textIndent")]
        public string TextIndent { get; set; }

        /// <summary>
        /// Gets or sets the text-justify value.
        /// </summary>
        [DomName("textJustify")]
        public string TextJustify { get; set; }

        /// <summary>
        /// Gets or sets the text-overflow value.
        /// </summary>
        [DomName("textOverflow")]
        public string TextOverflow { get; set; }

        /// <summary>
        /// Gets or sets the text-shadow value.
        /// </summary>
        [DomName("textShadow")]
        public string TextShadow { get; set; }

        /// <summary>
        /// Gets or sets the text-transform value.
        /// </summary>
        [DomName("textTransform")]
        public string TextTransform { get; set; }

        /// <summary>
        /// Gets or sets the text-underline-position value.
        /// </summary>
        [DomName("textUnderlinePosition")]
        public string TextUnderlinePosition { get; set; }

        /// <summary>
        /// Gets or sets the top value.
        /// </summary>
        [DomName("top")]
        public string Top { get; set; }

        /// <summary>
        /// Gets or sets the transform value.
        /// </summary>
        [DomName("transform")]
        public string Transform { get; set; }

        /// <summary>
        /// Gets or sets the transform-origin value.
        /// </summary>
        [DomName("transformOrigin")]
        public string TransformOrigin { get; set; }

        /// <summary>
        /// Gets or sets the transform-style value.
        /// </summary>
        [DomName("transformStyle")]
        public string TransformStyle { get; set; }

        /// <summary>
        /// Gets or sets the  value.
        /// </summary>
        [DomName("transition")]
        public string Transition { get; set; }

        /// <summary>
        /// Gets or sets the transition-delay value.
        /// </summary>
        [DomName("transitionDelay")]
        public string TransitionDelay { get; set; }

        /// <summary>
        /// Gets or sets the transition-duration value.
        /// </summary>
        [DomName("transitionDuration")]
        public string TransitionDuration { get; set; }

        /// <summary>
        /// Gets or sets the transition-property value.
        /// </summary>
        [DomName("transitionProperty")]
        public string TransitionProperty { get; set; }

        /// <summary>
        /// Gets or sets the transition-timing-function value.
        /// </summary>
        [DomName("transitionTimingFunction")]
        public string TransitionTimingFunction { get; set; }

        /// <summary>
        /// Gets or sets the unicode-bidi value.
        /// </summary>
        [DomName("unicodeBidi")]
        public string UnicodeBidi { get; set; }

        /// <summary>
        /// Gets or sets the vertical-align value.
        /// </summary>
        [DomName("verticalAlign")]
        public string VerticalAlign { get; set; }

        /// <summary>
        /// Gets or sets the visibility value.
        /// </summary>
        [DomName("visibility")]
        public string Visibility { get; set; }

        /// <summary>
        /// Gets or sets the white-space value.
        /// </summary>
        [DomName("whiteSpace")]
        public string WhiteSpace { get; set; }

        /// <summary>
        /// Gets or sets the widows value.
        /// </summary>
        [DomName("widows")]
        public string Widows { get; set; }

        /// <summary>
        /// Gets or sets the width value.
        /// </summary>
        [DomName("width")]
        public Length Width { get; set; }

        /// <summary>
        /// Gets or sets the word-break value.
        /// </summary>
        [DomName("wordBreak")]
        public string WordBreak { get; set; }

        /// <summary>
        /// Gets or sets the word-spacing value.
        /// </summary>
        [DomName("wordSpacing")]
        public string WordSpacing { get; set; }

        /// <summary>
        /// Gets or sets the writing-mode value.
        /// </summary>
        [DomName("writingMode")]
        public string WritingMode { get; set; }

        /// <summary>
        /// Gets or sets the z-index value.
        /// </summary>
        [DomName("zIndex")]
        public string ZIndex { get; set; }

        /// <summary>
        /// Gets or sets the zoom value.
        /// </summary>
        [DomName("zoom")]
        public string Zoom { get; set; }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
