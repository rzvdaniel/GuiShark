# HTML and CSS subset

AngleSharp parses HTML. GuiShark's bounded CSS parser resolves the supported properties below. This is not full CSS Flexbox or browser layout.

## Markup

Supported tags: `body`, `div`, `section`, `main`, `header`, `footer`, `p`, `span`, `h1`, `h2`, `h3`, `button`, and `img`.

- `id` identifies elements for C# lookup; duplicate IDs throw an error.
- `class` and inline `style` supply styling. `disabled` disables a button.
- `<link rel="stylesheet" href="styles.css">` and `<style>` load CSS in document order.
- `<img src="image.png">` stretches an image into its content box. Specify dimensions; the natural fallback size is 48 × 48, with container stretch rules still applying.
- Entities are decoded and whitespace is collapsed. Text is not duplicated into ancestors.
- Mixed text and child elements within a node are rejected; wrap text in its own `<span>`.
- Unknown tags, properties, selectors, values, CSS at-rules, and `!important` are rejected rather than silently ignored.

## Selectors and cascade

Type (`button`), class (`.primary`), ID (`#save`), universal (`*`), compounds (`button.primary`), comma-separated groups, and descendants (`.dark button`) are supported. State selectors are `:hover`, `:active`, `:focus`, and `:disabled`.

Rules apply by specificity, then document order; inline styles apply last. Color, font size, weight, and text alignment inherit. Buttons default to centered text. Hover applies to a hit node and its ancestors. Active/focus follow pointer and keyboard state. Attribute selectors, child/sibling combinators, pseudo-elements, and other pseudo-classes are unsupported.

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
| `border` | `none` or `1px solid #aabbcc` |
| `border-radius` | One nonnegative pixel radius |
| `opacity` | 0–1, multiplied into each primitive and descendant |
| `font-size` | Positive pixel size |
| `font-weight` | `normal`, `400`, `bold`, `600`, `700` |
| `text-align` | `left`, `center`, `right` |

Text uses the host's `FontBook` and a line height of 1.45 × font size. No font downloads occur. PNGs use `<img>`; background images, shadows, animations, font-family selection, text input/selection are not implemented.

The demo's macOS-inspired buttons use CSS gradients, borders, rounded corners, and pseudo-classes. Its logo demonstrates PNG rendering. All application behavior is C#.
