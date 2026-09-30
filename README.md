# GuiShark

Define a small interface in HTML and CSS, render it in an existing OpenGL application, and bind buttons to C# code.

The SDK is in `src/GuiShark` and `src/GuiShark.OpenGL`; all examples live in `src/demos`. It uses **.NET 10**, **AngleSharp 1.8.2**, **OpenTK 4.9.4**, **SkiaSharp 4.153.0**, and **FreeTypeSharp 3.1.0**. Shapes, borders, rounded corners, and gradients are drawn by OpenGL shaders. Pluggable text backends draw label bitmaps or glyph atlases; the engine does not render the whole UI to a bitmap. There is no embedded browser or JavaScript runtime.

![GuiShark's HTML/CSS playground rendered by OpenGL](docs/preview.png)

## Run the playground

Install a stable .NET 10 SDK and use a desktop with an OpenGL 3.3 core driver.

```powershell
dotnet build Gui.Shark.sln
dotnet run --project src/demos/GuiShark.Demo -- src/demos/GuiShark.Demo/Assets
```

Run from the repository root. Passing the source asset directory lets **F5** reload HTML/CSS edits. Without the argument, the demo uses assets copied beside the executable. The window is resizable, with a minimum size to keep this example readable.

- **Add a click** increments the counter using a C# event handler.
- **Switch to dark/light** changes a CSS class and the independent host scene.
- **Reset playground** restores the defaults; it is disabled until something changes.
- **Tab / Shift+Tab** navigate enabled buttons; **Enter / Space** activate them.
- **Escape** clears UI focus/capture; press again to close.
- **F5** reloads the document and resets demo state. Invalid CSS is reported in the console and leaves the existing UI running.
- **F12** saves a PNG of the actual framebuffer in `artifacts/` under the current working directory.

Edit [the HTML](src/demos/GuiShark.Demo/Assets/index.html), [the CSS](src/demos/GuiShark.Demo/Assets/styles.css), and [the callbacks](src/demos/GuiShark.Demo/DemoController.cs).

## Balloon game demo

The second demo, **Lantern Valley**, puts an illustrated woodland fantasy HTML/CSS HUD over an independent 3D OpenGL game. Fly a hot-air balloon over generated green hills, collect six lanterns, and use menus, pause controls and a translucent HUD. Large windows use fixed-size carved corners; buttons use leaf accents; compact HUD elements use restrained gold trim. Transparent artwork keeps its proportions while CSS surfaces expand with the controls.

```powershell
dotnet run --project src/demos/GuiShark.Balloon
```

Click the landscape to fly, scroll to zoom, and use **Find the next lantern** for guidance. See [controls and integration](docs/balloon-demo.md).

![Lantern Valley's HTML HUD over the OpenGL landscape](docs/balloon-hud.png)

## Text Lab

Compare **pixel-aligned Skia**, **FreeType glyph atlases**, and **MSDF scalable text** through the same SDK interface, with the original Skia path as a reference. Switch backends or view all four together; adjust size, weight, color, hinting, filtering, pixel snapping, fractional positioning, density, backgrounds and shadows. A nearest-pixel magnifier shows the actual framebuffer pixels.

```powershell
dotnet run --project src/demos/GuiShark.TextDemo
```

TTF/OTF files can be copied into your asset directory and loaded using CSS `@font-face` and `font-family`. See [text rendering, font loading and platform notes](docs/text-rendering.md).

MSDF atlases can also be generated during build/publish: configure `GUISHARK_MSDF_GENERATOR` with the native atlas generator's path, then build Text Lab normally. Generation is incremental and stays out of the shipped application. See [build-time setup for your own application](docs/text-rendering.md#generate-msdf-atlases-during-build).

![GuiShark Text Lab comparing four rendering paths](docs/text-lab.png)

## Controls Gallery

Try buttons, checkboxes, grouped radio buttons, sliders, labels, and progress bars in one place. Compare the optional neutral theme with an emerald CSS skin, inspect disabled/focus states, and watch C# value-change events in the live log.

```powershell
dotnet run --project src/demos/GuiShark.ControlsDemo
```

See [HTML controls, themes and input forwarding](docs/controls.md). No scrolling, dropdowns or text editing are included in this first set.

![GuiShark Controls Gallery rendered in OpenGL](docs/controls-gallery.png)

## Architecture

| Project | Responsibility |
| --- | --- |
| `GuiShark` | HTML loading, bounded CSS cascade, element state, layout, hit testing, input |
| `GuiShark.OpenGL` | Fonts, texture caches, GPU drawing, clipping, graphics-state restoration |
| `GuiShark.Demo` | Window, host background scene, input forwarding, application callbacks |
| `GuiShark.Balloon` | Independent 3D balloon game, procedural landscape, mouse steering, HTML menus/HUD |
| `GuiShark.ControlsDemo` | Interactive controls gallery, CSS skin examples and live state/event diagnostics |
| `GuiShark.TextDemo` | Backend comparison, density simulation, pixel magnification and observed cache/draw statistics |

The SDK does not own a window, swap buffers, clear the host framebuffer, or run a game loop. It can be used with another window/input library. See [embedding in your game](docs/embedding.md) and [the CSS subset](docs/css-subset.md).

```csharp
var assets = new DirectoryAssetSource("Assets");
var document = HtmlLoader.Load(assets.ReadText("index.html"), assets);
document.GetElement("increment").Clicked += button =>
{
    document.GetElement("count").Text = "Clicked from C#!";
};
```

This is an intentionally small retained UI engine. It has no scrolling, text editing, full inline layout, flex wrapping, animation system, or accessibility bridge. Windows rendering and interaction have been manually checked; Linux/macOS are not yet verified. No unit or integration tests have been added.

Code and project artwork use the repository's MIT license. Woodland asset provenance and generation prompts are in [artwork notes](docs/woodland-artwork.md). Bundled Lato fonts use the SIL Open Font License; see [third-party notices](THIRD-PARTY-NOTICES.md).
