# GuiShark text rendering

The supported implementation is **pixel-aligned Skia with HarfBuzz shaping**. `OpenGlUiRenderer(view, fonts)` uses it by default. Skia rasterizes transparent grayscale label masks at the device size; OpenGL composites these over the existing game framebuffer. `ITextBackend` remains the extension point for custom implementations.

The standalone FreeType and MSDF backends, the old resampled baseline, their atlases, native package references and generation tools have been retired. Their earlier implementation is available in Git history at `e0b37ac`. There is no atlas-build prerequisite for fonts now.

## Run Text Lab

```powershell
dotnet run --project src/demos/GuiShark.TextDemo
dotnet run --project src/demos/GuiShark.TextDemo -- --sample arabic
dotnet run --project src/demos/GuiShark.TextDemo -- --mode harfbuzz --sample indic
dotnet run --project src/demos/GuiShark.TextDemo -- --density 1.25 --background hills --shadow
```

**Compare shaping** shows two panes using the same Skia rasterizer: direct glyph mapping and HarfBuzz shaping. The right pane represents the SDK default. Arabic makes joining and direction differences particularly visible. Use the Sample button to cycle Latin, CJK, Indic, Arabic and mixed RTL specimens. Mixed RTL is diagnostic: full bidirectional paragraph layout is not implemented.

Controls adjust selected font size (8–48px), regular/bold weight, adaptive/gold/mint colors, simulated density (1/1.25/1.5/2×), pixel snapping, hinting, filtering, half-device-pixel positioning, shadows and backgrounds. The Latin matrix shows 10, 12, 14, 18, 24 and 36px samples; multilingual matrices show a smaller set of sizes for readability. Large specimens may clip within a comparison pane; select one rendering mode to give them more space. The bundled Noto families currently provide regular weight only, so Bold falls back to their regular face.

The magnifier enlarges actual framebuffer pixels at 2/4/8× using nearest filtering. This simulates density, not a physical HiDPI display. Each pane uses the same settings and background time, draws into its own host-owned framebuffer, and composites one-to-one. Text Lab chrome uses the default shaped Skia independently of the specimen options. Statistics show cache entries, GPU bytes, draws, uploads and CPU paint time; they exclude GPU completion time and are not a benchmark.

CLI modes are `compare|skia|harfbuzz`; samples are `latin|cjk|indic|arabic|mixed`. Existing size, hinting and filter flags remain: `--font-size 8..48`, `--density 1|1.25|1.5|2`, `--hinting none|slight|normal|full`, `--filter linear|nearest`, `--no-snap`, `--integer`, `--bold`, `--shadow`, `--background dark|light|hills`, `--size WIDTHxHEIGHT` (minimum 1180×900), and `--assets DIR`.

```powershell
dotnet run --project src/demos/GuiShark.TextDemo -- --sample arabic --capture artifacts/text-shaping.png
```

Capture mode warms 30 frames, freezes background time, saves the framebuffer and closes. Tab/Shift+Tab and Enter/Space operate controls; F12 captures; Escape clears focus, then closes.

![Skia with and without HarfBuzz shaping](text-lab.png)

## Load fonts with CSS

Copy TTF/OTF files beneath your asset root and copy assets into the build output:

```xml
<Content Include="Assets/**/*" CopyToOutputDirectory="PreserveNewest" />
```

```css
@font-face { font-family: "My UI"; src: url("fonts/MyUi-Regular.ttf"); font-weight: 400; }
@font-face { font-family: "My UI"; src: url("fonts/MyUi-Bold.ttf"); font-weight: 700; }
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
// After drawing your scene, on its OpenGL thread:
renderer.Render(framebufferWidth, framebufferHeight);
```

Fonts load directly from asset bytes at runtime. No download, OS font installation, prebuilt atlas or separate tool is needed. Paths resolve against the document's asset source. `FontBook.Load(document)` registers additional CSS families. Missing bold faces use the family's regular face; unknown families fail explicitly. Registered families are immutable; restart or create a new font book when font bytes change.

The current subset has no WOFF, italics, variable axes, remote URLs or font fallback lists. Multilingual demos link static regular-weight Noto font assets and their OFL licenses from `src/demos/SharedAssets/fonts`; [font provenance](../src/demos/SharedAssets/fonts/README.md).

## Configure the renderer

Pixel snapping and hinting are enabled by default. Bitmap dimensions match the physical texture size so the UI does not resample text to a fractional logical width. Curves still need grayscale antialiasing; alignment cannot make every font, size or display identical. Transparent HUDs do not use LCD subpixel rendering.

```csharp
using var backend = new SkiaTextBackend(fonts); // HarfBuzz enabled.
// For diagnostics only: new SkiaTextBackend(fonts, shaping: false).
using var view = new UiView(document, backend);
using var renderer = new OpenGlUiRenderer(view, backend); // Borrows backend.
renderer.TextOptions = new(PixelSnap: true, Hinting: TextHinting.Normal,
    Sampling: TextSampling.Linear);
```

Use one backend per renderer: density, options and caches are mutable. Font books can be shared. `SetTextBackend` replaces layout metrics and releases old GPU text textures. Backends are borrowed unless `ownsTextBackend: true` transfers ownership. Dispose renderer, view, backend and font book before destroying the GL context.

`ITextBackend` combines measurement, configuration, cache diagnostics and drawing through `ITextCanvas`. Text images are premultiplied RGBA masks; the renderer preserves the existing framebuffer and host graphics state. Skia keeps up to 128 cached label images. GPU textures unused for more than two frames are released. HarfBuzz shapers are owned by the backend and borrow its font book's typefaces.

## Shaping and editing limits

HarfBuzz handles OpenType substitutions and positioning for a single font/script/direction run. It is not a complete paragraph engine. GuiShark does not yet segment or reorder bidi runs, resolve font fallback, implement color emoji, or map carets/selections through shaped glyph clusters. Logical prefix measurement can be imperfect around contextual forms and ligatures. Ordinary labels wrap at whitespace; textareas preserve graphemes when wrapping. See [multilingual input and composition](multilingual-input.md).

## Platforms and migration

SkiaSharp 4.153.0 and matching SkiaSharp.HarfBuzz 4.153.0 load native font/rasterization/shaping libraries. HarfBuzzSharp 14.2.1.300 supplies Win32/macOS native assets; the SDK explicitly references matching Linux native assets. Controls Gallery uses Silk.NET.SDL 2.23.0 with SDL 2.32.10; the other demos retain OpenTK. A desktop display, OpenGL 3.3 core driver and the chosen host's OS dependencies are required. Native Linux/macOS execution and real OS IME/HiDPI behavior remain unverified; [validation results](multilingual-validation.md).

Applications using the default renderer need no initialization changes. Applications using retired classes (`FreeTypeTextBackend`, `MsdfTextBackend`, `MsdfAtlas`) must switch to Skia. The old `legacyBaseline` constructor option and the canvas `distanceRange` argument are removed; the optional second constructor argument is now `shaping`. Remove MSDF target imports and font-generation items from your projects. Gallery `--text` and Text Lab `baseline|freetype|msdf` modes have been removed.
