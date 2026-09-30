# HTML and CSS subset

AngleSharp parses HTML. GuiShark's bounded CSS parser resolves the supported properties below. This is not full CSS Flexbox or browser layout.

## Markup

Supported tags: `body`, `div`, `section`, `main`, `header`, `footer`, `p`, `span`, `h1`, `h2`, `h3`, `button`, and `img`.

- `id` identifies elements for C# lookup; duplicate IDs throw an error.
- `class` and inline `style` supply styling. `disabled` disables a button.
- `<link rel="stylesheet" href="styles.css">` and `<style>` load CSS in document order.
- `<img src="image.png">` draws a transparent image into its content box. Specify dimensions; the natural fallback size is 48 × 48, with container stretch rules still applying. `object-fit` controls scaling.
- Entities are decoded and whitespace is collapsed. Text is not duplicated into ancestors.
- Mixed text and child elements within a node are rejected; wrap text in its own `<span>`.
- Unknown tags, properties, selectors, values, CSS at-rules, and `!important` are rejected rather than silently ignored.

## Selectors and cascade

Type (`button`), class (`.primary`), ID (`#save`), universal (`*`), compounds (`button.primary`), comma-separated groups, and descendants (`.dark button`) are supported. State selectors are `:hover`, `:active`, `:focus`, and `:disabled`.

Rules apply by specificity, then document order; inline styles apply last. Color, font size, weight, text alignment, and text shadow inherit. Buttons default to centered text. Hover applies to a hit node and its ancestors. Active/focus follow pointer and keyboard state. Attribute selectors, child/sibling combinators, pseudo-elements, and other pseudo-classes are unsupported.

## Layout

All dimensions use **border-box** semantics. Every element is a column container by default; `<span>` is not browser-inline. Set `flex-direction: row` for horizontal layout. `display: block` and `display: flex` both use this engine's container layout; `display: none` hides a subtree.

| Property | Values |
| --- | --- |
| `width`, `height`, `max-width` | Nonnegative pixels, unitless pixels, percentages, `auto` |
| `padding`, `margin` | One to four nonnegative pixel lengths, CSS shorthand order |
| `gap` | Nonnegative pixel length |
| `flex-direction` | `column`, `row` |
| `align-items` | `stretch`, `flex-start`, `center`, `flex-end` |
| `justify-content` | `flex-start`, `center`, `flex-end`, `space-between` |
| `flex-grow` | Nonnegative number; distributes remaining width in a row |
| `box-sizing` | `border-box` only |
| `position` | `static` (normal flow), `absolute` (anchored to immediate parent content box) |
| `top`, `right`, `bottom`, `left` | Nonnegative pixels, percentages, or `auto` |
| `pointer-events` | `auto`, `none`; inherits; children can explicitly restore `auto` |

Auto-width column children stretch with `align-items: stretch`; otherwise they use natural widths. Auto-width row children use natural widths. Auto heights derive from text or children. Percentage lengths resolve against available parent space; use explicit parent heights for predictable percentage heights. The root body always fills the viewport.

Rows do not wrap or shrink. Size children so widths, margins, and gaps fit. Overflow is clipped to ancestor content rectangles. Rounded corners affect paint, but child clipping/hit testing are rectangular. Long unbroken words are clipped rather than split. Scrolling, relative/fixed positioning, floats, grid, z-index, transforms, and margin collapsing are unsupported. Paint and hit-test order follow document order; children paint after parent backgrounds.

Absolutely positioned children are removed from flow. Each anchors to its immediate parent's content box, regardless of the parent's position (a deliberately simpler rule than browser containing blocks). With auto width and both left/right anchors, width fills the remaining space; the analogous rule applies to height with top/bottom. Absolute margins are not applied. Draw order is still document order, so place overlays last or use separate views for modal layers.

`pointer-events: none` makes decorative HUD regions transparent to hit testing while preserving rendering. Descendants inherit it, but a button can set `pointer-events: auto` to remain interactive. Button navigation skips nodes whose resolved pointer events are disabled. Visual transparency by itself does not change hit testing.

## Appearance

| Property | Values |
| --- | --- |
| `color`, `background-color`, `border-color` | `#RGB`, `#RRGGBB`, `#RRGGBBAA`, `white`, `black`, `transparent` |
| `background` | A supported color or `linear-gradient(to bottom, color1, color2)` |
| `background-image` | One `url("asset/path.png")`, unquoted URL, or `none` |
| `background-size`, `object-fit` | `fill` (default), `contain`, `cover`; centered, no tiling |
| `-guishark-background-slice` | One to four nonnegative source-image pixel lengths, CSS shorthand order |
| `-guishark-background-slice-width` | One to four destination lengths in logical UI pixels; defaults to source slice lengths |
| `-guishark-background-inset` | One to four nonnegative logical pixel lengths; inset only the background color/gradient |
| `-guishark-image-tint` | Supported color; multiplies image RGB/alpha, without changing text or background colors |
| `border` | `none` or `1px solid #aabbcc` |
| `border-radius` | One nonnegative pixel radius |
| `opacity` | 0–1, multiplied into each primitive and descendant |
| `font-size` | Positive pixel size |
| `font-weight` | `normal`, `400`, `bold`, `600`, `700` |
| `text-align` | `left`, `center`, `right` |
| `text-shadow` | `none` or one sharp shadow: nonnegative pixel X/Y offsets and a supported color, e.g. `1px 1px #000000cc` |

Text uses the host's `FontBook` and a line height of 1.45 × font size. No font downloads occur. Text shadows reuse the glyph texture and follow the same content clipping as text; blur, multiple shadows and box shadows are unsupported. Animations, font-family selection, text input/selection are not implemented.

## Illustrated controls

PNG alpha is preserved, decoded to premultiplied RGBA, and blended over the host framebuffer. Transparent pixels do not erase the game. Images are cached per renderer until disposal. Asset paths use the document's `IAssetSource` root, including URLs in a stylesheet; they are not relative to the stylesheet directory.

Background images paint above background colors/gradients and the border, below text and children, across the element's border box. `<img>` uses its content box. `contain` keeps the whole image with transparent space around it; `cover` crops the center to fill; `fill` stretches. Positioning, repeating, multiple image layers and intrinsic image layout are unsupported. `fill` is an engine extension for `background-size`. The color/gradient-only `background` shorthand clears `background-image`; `background-color` leaves it in place.

```css
.frame {
    background-color: #102e27ed;
    -guishark-background-inset: 35px;
    background-image: url("art/woodland-frame.png");
    -guishark-background-slice: 340px;
    -guishark-background-slice-width: 82px;
    padding: 54px;
}
button:hover { -guishark-image-tint: #ffffff; }
.icon { width: 52px; height: 65px; object-fit: contain; }
```

A nonzero slice enables nine-slice drawing: four corners, four stretched edges, and a stretched center. Source slices measure pixels in the original bitmap; destination widths measure logical UI pixels, independent of DPI. Opposite borders are reduced proportionally when their sum exceeds the image or destination size. Slices override `background-size` and use rectangular clipping; use the PNG's own transparent silhouette for ornate corners. The center is drawn, so leave it transparent in the asset when it should reveal a background color. These prefixed properties are GuiShark extensions, not browser `border-image` syntax.

Backgrounds do not affect layout or hit testing. Transparent parts of an interactive button still belong to its rectangular hit box. Use `pointer-events: none` on decorative elements and restore `auto` on controls. Image tint is white by default and only affects that element's images; normal element opacity still multiplies into images and descendants.

A transparent frame does not create a panel fill. Set a background color explicitly if you want one. `-guishark-background-inset` moves the color/gradient rectangle inward while the background image and CSS border keep the full element bounds. It changes neither content layout nor hit testing. `border-radius` rounds the inset fill; nine-slice image corners still use their own alpha. This is an authored rectangular inset, not an automatic silhouette mask: choose inset/radius values that tuck beneath the artwork's inner rim. Lantern Valley uses this to fill panels without leaking color outside their frames, plus a sharp text shadow for readability.

The demo's macOS-inspired buttons use CSS gradients, borders, rounded corners, and pseudo-classes. Its logo demonstrates PNG rendering. All application behavior is C#.
