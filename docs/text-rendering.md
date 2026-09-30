# GuiShark text rendering

GuiShark now uses `ITextBackend` for both layout measurement and text drawing. The default is pixel-aligned Skia. Existing `OpenGlUiRenderer(view, fonts)` hosts get the improvement without adopting a new windowing library. The original Skia path is retained only as a selectable comparison backend.

## Run Text Lab

```powershell
dotnet run --project src/demos/GuiShark.TextDemo
```

Choose **Compare all** for a two-by-two comparison or select one backend. The controls change selected font size (8–48px), regular/bold weight, adaptive/gold/mint colors, density (1/1.25/1.5/2×), pixel snapping, hinting, linear/nearest filtering, half-device-pixel positioning, sharp shadows, and dark/light/moving hill backgrounds. The HTML specimen includes sizes 10, 12, 14, 18, 24 and 36px, Latin accents, digits and punctuation. A centered HTML button is shown when the pane has room. Compact panes use a single row of six size specimens instead of clipping the larger letters. Large selected sizes can wrap or clip within the specimen viewport; select one backend to give them more space.

The magnifier samples a crop around the selected line at 2/4/8× using nearest filtering, so each source framebuffer pixel becomes a square. Larger magnifications deliberately show a smaller crop. Tab/Shift+Tab and Enter/Space operate the controls. F12 saves a screenshot; Escape clears UI focus, then closes.

Comparison panes draw into separate host-owned OpenGL framebuffers and composite **one-to-one**, without downsampling the text. Density multiplies the native framebuffer scale and changes each pane's logical dimensions. This is a density simulation, not verification of a physical HiDPI monitor. Chrome always uses default Skia and the native scale, independently of the specimen options. All panes share the same setting values and background time; metrics and spacing follow each actual backend.

```powershell
dotnet run --project src/demos/GuiShark.TextDemo -- --capture artifacts/text-lab.png
dotnet run --project src/demos/GuiShark.TextDemo -- --density 1.25 --background hills --shadow
dotnet run --project src/demos/GuiShark.TextDemo -- --mode msdf --font-size 36 --bold
```

Capture mode warms the renderer for 30 frames, saves the actual framebuffer, and closes. It freezes background time for review. Optional flags: `--assets DIR`, `--size WIDTHxHEIGHT` (minimum 1180×900), `--no-snap`, `--integer`, `--bold`, `--shadow`, `--background dark|light|hills`, `--hinting none|slight|normal|full` and `--filter linear|nearest`. Backend modes are `compare|baseline|skia|freetype|msdf`.

![Four real text rendering paths at native size](text-lab.png)

## Approaches and tradeoffs

| Backend | Pipeline | Useful for | Limits |
| --- | --- | --- | --- |
| Skia baseline | Whole-label bitmap, original logical placement, linear sampling and resized texture width | Seeing the previous behavior | Deliberately ignores hint/snap/filter controls |
| Skia pixel aligned | Grayscale rasterization at device size, explicit hinting, optional integer origins/baselines, exact texture dimensions | Small fixed-size menus and HUD labels; default | Re-rasterizes when size or density changes; whole-label textures |
| FreeType glyph atlas | FreeTypeSharp 3.1.0; hinted grayscale glyphs packed into padded RGBA atlas pages | Reused glyphs, numbers and changing HUD labels | Integer device font sizes; native library needed; per-glyph draw calls |
| MSDF scalable atlas | Baked multi-channel distance fields; derivative-based GLSL reconstruction | Large labels and scalable text without size-specific bitmaps | No hinting; linear filtering required; prebuilt font/charset; small text can appear lighter |

These are three rendering approaches, rather than a guarantee of three unrelated underlying font engines. Skia can use platform-specific font implementations. The common contract allows another backend to be added without changing DOM/layout/input. Hinting names are not a promise of identical output: FreeType Slight selects LIGHT targeting, Normal uses the font's default hinter, Full forces auto-hinting, and None disables hinting. Skia uses its corresponding `SKFontHinting` setting. MSDF ignores hint/filter controls and states that in its pane description. [FreeType's glyph API](https://freetype.org/freetype2/docs/reference/ft2-glyph_retrieval.html) and [the atlas generator](https://github.com/Chlumsky/msdf-atlas-gen) describe those mechanisms.

Observed draw calls, uploads, cache entries, GPU bytes, and CPU paint time appear per pane. They exclude chrome, host composition, background rendering, and GPU completion time. They are diagnostic observations, **not a benchmark**. The FreeType backend currently emits one draw per visible glyph (and another for its shadow); batching is a possible future optimization.

## Why the old text looked soft

The old label path rounded bitmap dimensions up but displayed them at the original logical width, introducing a small resize. Fractional UI coordinates, fractional baselines and linear sampling could interpolate those bitmap pixels again. The new Skia path preserves the bitmap's exact device dimensions, uses explicit hinting, and can align its origin and baselines to device pixels. Nearest sampling is available for comparison, rather than forced for all text or artwork.

Pixel alignment improves fixed-size UI text; it cannot make every font, size, transform and display identical. Grayscale anti-aliasing remains necessary for curved glyph outlines. GuiShark does not implement LCD subpixel rendering: its transparent HUD can be composited onto arbitrary backgrounds and displays. Fractional size/density can legitimately produce different glyph weights and advances. Uniform host scaling is recommended; nonuniform scaling can resample bitmap glyphs.

## Load fonts with CSS

Copy `.ttf` or `.otf` files beneath your application's asset root and include them in the build output:

```xml
<Content Include="Assets/**/*" CopyToOutputDirectory="PreserveNewest" />
```

```css
@font-face {
    font-family: "My UI";
    src: url("fonts/MyUi-Regular.ttf");
    font-weight: 400;
}
@font-face {
    font-family: "My UI";
    src: url("fonts/MyUi-Bold.ttf");
    font-weight: 700;
}
body { font-family: "My UI"; font-size: 14px; }
button { font-weight: bold; }
```

```csharp
var assets = new DirectoryAssetSource(assetDirectory);
var document = HtmlLoader.Load(assets.ReadText("menu.html"), assets);
using var fonts = new FontBook(document, "My UI");
using var view = new UiView(document, fonts);
using var renderer = new OpenGlUiRenderer(view, fonts);
view.Resize(logicalWidth, logicalHeight);
// Inside your existing GL game loop, after drawing the scene:
renderer.Render(framebufferWidth, framebufferHeight);
```

CSS paths resolve against the document's asset source, including declarations inside linked stylesheets. Nothing is downloaded or installed into the OS. `FontBook.Load(document)` adds CSS families to an existing host font book. A missing bold face uses the family's regular face; unknown families fail explicitly. Registered families are immutable: create a new font book to reload changed font bytes. The current subset has no WOFF, italics, font fallback lists, variable axes, or remote URLs.

Skia and FreeType load font bytes directly. **MSDF requires a matching prebuilt PNG/JSON atlas**, so merely replacing the TTF does not replace its glyph artwork. Its constructor takes regular/bold atlases and a family name; incompatible family selections fail explicitly. The caller must keep the atlases and the chosen font files consistent.

## Select or implement a backend

```csharp
fonts.Load(document); // Before layout when using CSS font families.
using var backend = new FreeTypeTextBackend(fonts);
using var view = new UiView(document, backend);
using var renderer = new OpenGlUiRenderer(view, backend); // Borrows backend.
renderer.TextOptions = new(PixelSnap: true, Hinting: TextHinting.Normal);
```

`renderer.SetTextBackend(nextBackend)` changes layout metrics and releases old GPU text textures. The caller owns borrowed backends; pass `ownsTextBackend: true` to transfer ownership. Use one backend per renderer because its raster scale and caches are mutable. Fonts may be shared. Dispose renderer, view, backend and font book before destroying the context.

`ITextBackend` extends `ITextMetrics`. `Configure` establishes density/options and reports metric changes; `Draw` submits `TextImage` + source/destination rectangles to `ITextCanvas`. CPU font/rasterization code has no window/context dependency; `OpenGlTextCanvas` owns GPU textures and revision-based uploads. Atlas masks are premultiplied RGBA coverage; MSDF images contain raw RGB distance data in a non-sRGB RGBA8 texture. The shader generates premultiplied color for the existing UI blend path. Drawing preserves the existing framebuffer and host graphics-state behavior.

Skia caches at most 128 label images. FreeType uses 1024² pages with gutters and a 16-page/64 MiB atlas budget; exceeding it fails clearly instead of invalidating live glyphs. Density/hint changes reset that atlas. GPU images unused for more than two frames are released. Image textures and font bytes are outside the displayed text cache totals.

The comparison uses Unicode scalar enumeration for atlas paths, basic LTR word wrapping, and no pair kerning/ligatures. It is not a full typography engine: no complex-script shaping, bidi, emoji or font fallback. **HarfBuzz** is the portable shaping component to introduce before supporting Arabic/Indic scripts and advanced OpenType positioning; it is deliberately not presented as an implemented fourth rasterizer. [HarfBuzz's manual](https://harfbuzz.github.io/what-is-harfbuzz.html) explains its role.

## Platforms and native assets

There is no DirectWrite, GDI or other Windows-only text path. Skia and FreeType use cross-platform packages; MSDF needs no additional native rasterizer at runtime, although the SDK's image/font infrastructure still uses Skia. FreeTypeSharp 3.1.0 ships its patched FreeType 2.13.2; do not replace it with an arbitrary system library because its ABI must match the bindings. [FreeTypeSharp's source](https://github.com/ryancheung/FreeTypeSharp) identifies the patched build.

| Platform | FreeTypeSharp 3.1.0 package inspected here | Native rendering status |
| --- | --- | --- |
| Windows x64 | `freetype.dll`; also ships x86/ARM64 binaries | Windows x64 OpenGL captures reviewed |
| Linux x64 | `libfreetype.so` | Packaging checked; native rendering unverified |
| macOS x64 / Apple Silicon | `libfreetype.dylib`, universal x64/ARM64 Mach-O | Packaging checked; native rendering unverified |
| Linux ARM64 | No matching FreeType native binary in this package | FreeType backend unavailable without a compatible build; other choices remain available |

Text Lab displays an unavailable pane if a native dependency cannot load; it never silently substitutes Skia while calling it FreeType. A functioning OpenGL 3.3 core context and the host/window library's OS dependencies are still required. Release build passed; framework-dependent publishes for Windows x64, Linux x64, macOS x64 and macOS ARM64 contain the expected Skia/FreeType/GLFW native files. Windows captures were reviewed at 1×, 1.25×, 1.5× and 2× simulated density, including small-window, shadow, bold, bitmap and large MSDF specimens; Lantern Valley still renders with the improved default. The user confirmed Text Lab controls work. Native Linux/macOS execution and real HiDPI display behavior remain unverified. No unit or integration tests were added.

## Rebuild the MSDF assets

The included atlases use **msdf-atlas-gen 1.4 / MSDFgen 1.13**, bundled Lato Regular/Bold, 48 pixels/em, a 4-pixel distance range, bottom-origin metadata, and no pair kerning. The requested charset is U+0020–017F plus U+2010–2026. Each font supplies 338 glyphs; controls U+007F–009F and U+2011/2023/2024/2025 are absent. Unsupported scalars render `?` in the atlas backends; this is visible fallback, not full Unicode coverage.

Download the official generator release or build it from source, then run with PowerShell 7 on any supported platform:

```powershell
./tools/generate-text-atlases.ps1 -Generator /path/to/msdf-atlas-gen
```

The equivalent command per font is:

```text
msdf-atlas-gen -font Lato-Regular.ttf -chars "[0x20,0x17f], [0x2010,0x2026]" -type msdf -format png -size 48 -pxrange 4 -yorigin bottom -nokerning -imageout Lato-Regular.png -json Lato-Regular.json
```

Keep the font OFL notice when redistributing the derived atlases. MSDFgen's shader formula and generator licenses are retained under `licenses/`; see [third-party notices](../THIRD-PARTY-NOTICES.md).
