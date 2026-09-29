# GuiShark

Define a small interface in HTML and CSS, render it in an existing OpenGL application, and bind buttons to C# code.

The modern implementation is in `src/`. It uses **.NET 10**, **AngleSharp 1.8.2**, **OpenTK 4.9.4**, and **SkiaSharp 4.153.0**. Shapes, borders, rounded corners, and gradients are drawn by OpenGL shaders. SkiaSharp rasterizes font and image textures; it does not render the whole UI to a bitmap. There is no embedded browser or JavaScript runtime.

![GuiShark's HTML/CSS playground rendered by OpenGL](docs/preview.png)

## Run the playground

Install a stable .NET 10 SDK and use a desktop with an OpenGL 3.3 core driver.

```powershell
dotnet build Gui.Shark.sln
dotnet run --project src/GuiShark.Demo -- src/GuiShark.Demo/Assets
```

Run from the repository root. Passing the source asset directory lets **F5** reload HTML/CSS edits. Without the argument, the demo uses assets copied beside the executable. The window is resizable, with a minimum size to keep this example readable.

- **Add a click** increments the counter using a C# event handler.
- **Switch to dark/light** changes a CSS class and the independent host scene.
- **Reset playground** restores the defaults; it is disabled until something changes.
- **Tab / Shift+Tab** navigate enabled buttons; **Enter / Space** activate them.
- **Escape** clears UI focus/capture; press again to close.
- **F5** reloads the document and resets demo state. Invalid CSS is reported in the console and leaves the existing UI running.
- **F12** saves a PNG of the actual framebuffer in `artifacts/` under the current working directory.

Edit [the HTML](src/GuiShark.Demo/Assets/index.html), [the CSS](src/GuiShark.Demo/Assets/styles.css), and [the callbacks](src/GuiShark.Demo/DemoController.cs).

## Small, separate responsibilities

| Project | Responsibility |
| --- | --- |
| `GuiShark` | HTML loading, bounded CSS cascade, element state, layout, hit testing, input |
| `GuiShark.OpenGL` | Fonts, texture caches, GPU drawing, clipping, graphics-state restoration |
| `GuiShark.Demo` | Window, host background scene, input forwarding, application callbacks |

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

## Original prototype

The 2018 `Gui.Shark.*` project directories are preserved unchanged. Open `Gui.Shark.Legacy.sln` to inspect that version. The main `Gui.Shark.sln` builds only the modern SDK and demo. There is no binary compatibility promise with the prototype's `TElement` / `TGame` APIs.

Code and original shark artwork use the repository's MIT license. Bundled Lato fonts use the SIL Open Font License; see [third-party notices](THIRD-PARTY-NOTICES.md).
