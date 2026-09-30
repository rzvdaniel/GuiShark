# Woodland artwork

Generated with the built-in image generation tool for Lantern Valley. These are project assets, not extracted franchise artwork. The generated RGBA files are copied unchanged into the repository; transparency is preserved. They are distributed under the repository's MIT license. No CLI/API fallback was used.

| Asset | Current use and logical display size |
| --- | --- |
| `src/GuiShark.Balloon/Assets/art/woodland-corner.png` | Menu/modal corners, 128 × 128; mirrored for the other three corners |
| `src/GuiShark.Balloon/Assets/art/woodland-leaf.png` | Button accents 52 × 52; guidance accents 40 × 40; objective accent 56 × 56 |
| `src/GuiShark.Balloon/Assets/art/lantern-relic.png` | Menu emblem 88 × 104, HUD relic 64 × 84; aspect ratio preserved with `contain` |
| `src/GuiShark.Balloon/Assets/art/woodland-frame.png` | Earlier full-frame study, retained as optional artwork; not used by the current skin |
| `src/GuiShark.Balloon/Assets/art/woodland-button.png` | Earlier illustrated button study, retained as optional artwork; not used by the current skin |

Display sizes and tints are in `src/GuiShark.Balloon/Assets/styles.css`. The SDK caches each used image once per renderer; four corner nodes share one texture and two button accents share one leaf texture. The artwork contains no text; all labels are rendered from HTML. The compact six-slot progress indicator uses shader-drawn shapes instead of miniature copies of the detailed relic.

## Size artwork for its job

Use detailed corners on large windows, a simple gold border on the collection HUD, and one readable leaf accent on the objective strip. The small Pause button uses plain trim. Only colors, borders and gradients expand with the control; corner and leaf images keep explicit square dimensions and `object-fit: contain`. No ornate image is stretched across a panel or button.

Judge artwork at its intended logical display size, not just at source resolution. Design a 52px ornament around two broad leaves rather than many tiny vines. A higher-resolution PNG supplies sharper pixels on high-DPI displays; it does not make tiny details readable at the same apparent size. For a 128px ornament on a 2× framebuffer, supply at least 256 source pixels across it while retaining a CSS width of 128px. Increase the logical size, reduce decorative complexity, or move rich artwork to a larger view when more detail is needed.

Nine-slice remains available in the SDK for suitable assets. Use equal source-to-destination ratios on both axes to preserve corner proportions, and keep edge strips plain; nine-slice does not preserve detail if corner regions themselves are reduced heavily. The refined demo uses separately positioned ornament and CSS surfaces, so repeating textured edges are unnecessary.

The menu and HUD were captured at 1200 × 820 and 920 × 680. Corner images remain 128 × 128 at both sizes. This checks composition at 1×, not physical high-DPI behavior.

## Corner prompt

Use case: stylized-concept. Create ONE isolated TOP LEFT corner ornament for an original woodland elf game menu, on a square true transparent RGBA canvas. Intended display size is 112 by 112 logical pixels: use bold readable shapes, not miniature engraving. A curved carved walnut L-shaped corner with aged gold edging, THREE large emerald leaves curling diagonally inward, and ONE prominent oval cyan moonstone roughly one quarter of the ornament width. Hand painted fantasy game illustration, rich warm wood and gold, crisply separated materials. The ornament occupies the upper and left sides, with its broad curl concentrated around the upper-left diagonal. Bottom-right region must be mostly empty transparent space; this is one corner, not a complete frame or badge. Equal horizontal and vertical extent so it can be mirrored to the other corners without anisotropic stretching. Fill about 90% of the square with 5% transparent safety margin. At 112px each leaf and gemstone must remain clearly visible. No fine runic lettering, no text, no logo, no backdrop, no franchise designs.

## Leaf accent prompt

Use case: stylized-concept. ONE small woodland fantasy game UI accent on true transparent RGBA square canvas, designed to read clearly at 48 to 56 pixels. TWO broad emerald leaves with simple large gold veins, gracefully curving together around ONE small amber gem at the base. A clean compact asymmetric diagonal sprig, no frame, no button surface, no background, no shadow beyond silhouette. Hand painted carved leaf and aged gold style. Big simple material shapes, bold outer silhouette, minimal engraving, no tiny decorations or dense vines. The sprig fills 85 percent of the square. It will sit beside HTML text in buttons and on a compact quest strip; keep most mass toward the center and no long thin tail. No text, logos, watermarks, franchise elements.

## Earlier full-frame prompt

Use case: stylized-concept. Create ONE production game UI asset: an ornate woodland elf rectangular panel frame, front view, square 1024x1024 canvas, true transparent RGBA background. Outer carved dark walnut wood frame with aged gold thin inlays, emerald leaves and curling vines concentrated in four corners, tiny cyan moonstone gems. Symmetrical elegant fantasy RPG painted illustration, finely textured materials, warm hand painted light. The frame fits inside the canvas with 20px transparent outer margin; border thickness roughly 110px. CENTRAL 760x760 AREA MUST BE COMPLETELY TRANSPARENT, empty hole, no fill, so live HTML text can be composited over a dark background behind it. Keep straight middle edge strips simple and uniform so this asset can use nine slice scaling. No lettering, no logos, no watermark, no backdrop, no drop shadow outside the canvas. Original art, no existing franchise designs.

## Earlier button prompt

Use case: stylized-concept. ONE production fantasy RPG UI button asset on a true transparent RGBA canvas, landscape 1536x1024. Centered horizontal button occupies x=96..1440 y=320..704 (roughly 3.5:1 shape). Carved dark forest teal wood surface with a thin burnished gold double rim, small symmetrical gold leaves and emerald vine curls at left and right ends, tiny turquoise moonstone gem at each end. Front view flat painted game UI, rich hand painted realistic textures. The central surface is dark smooth mostly blank, suitable for white HTML button label overlaid by game; NO text or lettering drawn in the image. Transparent outside silhouette. Keep vertical middle strip uniform horizontally for nine slice resizing, ornate endcaps. No logos or franchise assets, no watermark, no perspective. Original elegant woodland elf theme.

## Lantern prompt

Use case: stylized-concept. ONE transparent RGBA game inventory icon, square canvas. Original woodland elf lantern relic, front three-quarter view, an elegant small hanging lantern with ornate aged gold and dark carved wood cage, emerald curling leaves clasping its shoulders, a warm amber floating light inside, a tiny cyan moonstone at the top. Hand painted high quality fantasy RPG item illustration, crisp silhouette, warm internal glow kept close to the object, no scene or background, true transparent pixels around the icon. Object centered, fills 85 percent of canvas height, no UI frame, no letters, no watermark, no franchise designs. Readable at 80px tall.
