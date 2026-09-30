# Woodland artwork

Generated with the built-in image generation tool for Lantern Valley. These are project assets, not extracted franchise artwork. The generated RGBA files are copied unchanged into the repository; transparency is preserved. They are distributed under the repository's MIT license. No CLI/API fallback was used.

| Asset | Purpose |
| --- | --- |
| `src/GuiShark.Balloon/Assets/art/woodland-frame.png` | Transparent panel border, nine-sliced at 340 source pixels |
| `src/GuiShark.Balloon/Assets/art/woodland-button.png` | Leaf/gold button, nine-sliced at 260 vertical / 430 horizontal source pixels |
| `src/GuiShark.Balloon/Assets/art/lantern-relic.png` | Menu emblem and HUD collection icons, rendered with `object-fit: contain` |

Destination widths and tints are in `src/GuiShark.Balloon/Assets/styles.css`. The SDK caches each image once per renderer; the six HUD slots reuse the lantern texture. The artwork contains no text; all labels are rendered from HTML.

## Frame prompt

Use case: stylized-concept. Create ONE production game UI asset: an ornate woodland elf rectangular panel frame, front view, square 1024x1024 canvas, true transparent RGBA background. Outer carved dark walnut wood frame with aged gold thin inlays, emerald leaves and curling vines concentrated in four corners, tiny cyan moonstone gems. Symmetrical elegant fantasy RPG painted illustration, finely textured materials, warm hand painted light. The frame fits inside the canvas with 20px transparent outer margin; border thickness roughly 110px. CENTRAL 760x760 AREA MUST BE COMPLETELY TRANSPARENT, empty hole, no fill, so live HTML text can be composited over a dark background behind it. Keep straight middle edge strips simple and uniform so this asset can use nine slice scaling. No lettering, no logos, no watermark, no backdrop, no drop shadow outside the canvas. Original art, no existing franchise designs.

## Button prompt

Use case: stylized-concept. ONE production fantasy RPG UI button asset on a true transparent RGBA canvas, landscape 1536x1024. Centered horizontal button occupies x=96..1440 y=320..704 (roughly 3.5:1 shape). Carved dark forest teal wood surface with a thin burnished gold double rim, small symmetrical gold leaves and emerald vine curls at left and right ends, tiny turquoise moonstone gem at each end. Front view flat painted game UI, rich hand painted realistic textures. The central surface is dark smooth mostly blank, suitable for white HTML button label overlaid by game; NO text or lettering drawn in the image. Transparent outside silhouette. Keep vertical middle strip uniform horizontally for nine slice resizing, ornate endcaps. No logos or franchise assets, no watermark, no perspective. Original elegant woodland elf theme.

## Lantern prompt

Use case: stylized-concept. ONE transparent RGBA game inventory icon, square canvas. Original woodland elf lantern relic, front three-quarter view, an elegant small hanging lantern with ornate aged gold and dark carved wood cage, emerald curling leaves clasping its shoulders, a warm amber floating light inside, a tiny cyan moonstone at the top. Hand painted high quality fantasy RPG item illustration, crisp silhouette, warm internal glow kept close to the object, no scene or background, true transparent pixels around the icon. Object centered, fills 85 percent of canvas height, no UI frame, no letters, no watermark, no franchise designs. Readable at 80px tall.
