# Embedding GuiShark

Reference `src/GuiShark.OpenGL/GuiShark.OpenGL.csproj` from a .NET 10 application. Its dependency brings in `GuiShark`. Only the demo depends on OpenTK windowing; the renderer uses `OpenTK.Graphics` without creating a window.

## Context and lifetime

The backend requires desktop **OpenGL 3.3 core or newer**. OpenGL ES, WebGL, Vulkan, and Direct3D are not supported. macOS needs a forward-compatible core context. Windows has been manually verified; other platforms remain unverified.

Create the host context and make it current first. If your host does not use OpenTK windowing, load OpenTK's entry points with the host's procedure-address resolver:

```csharp
using OpenTK;
using OpenTK.Graphics.OpenGL4;

sealed class HostBindings(Func<string, IntPtr> getProcAddress) : IBindingsContext
{
    public IntPtr GetProcAddress(string name) => getProcAddress(name);
}

// While your existing context is current (substitute your library's resolver):
GL.LoadBindings(new HostBindings(name => yourWindowLibrary.GetProcAddress(name)));
```

OpenTK's `GameWindow` already loads these bindings. GuiShark neither owns the context nor initializes the host's input library.

```csharp
using GuiShark;
using GuiShark.OpenGL;

var assets = new DirectoryAssetSource(assetDirectory);
using var fonts = new FontBook(
    Path.Combine(assetDirectory, "fonts/Lato-Regular.ttf"),
    Path.Combine(assetDirectory, "fonts/Lato-Bold.ttf"));
var document = HtmlLoader.Load(assets.ReadText("index.html"), assets);
using var ui = new UiView(document, fonts);
using var renderer = new OpenGlUiRenderer(ui, fonts);

var count = 0;
document.GetElement("increment").Clicked += _ =>
{
    document.GetElement("count").Text = (++count).ToString();
};
ui.Resize(logicalWidth, logicalHeight);

// Inside the host frame loop:
DrawYourGame();
ui.Update(); // Render and input entry points also call this automatically.
renderer.Render(framebufferWidth, framebufferHeight);
SwapYourBuffers();
```

Construct, use, and dispose on the render/UI thread. The SDK is not thread-safe; marshal background updates to that thread. Each renderer belongs to one GL context. Dispose **before destroying the context**, in order: renderer, view, fonts. The renderer borrows the view and font book; it does not dispose them. `UiView.Dispose` removes element-change subscriptions. Use one document per view because resolved styles and bounds live on its nodes.

## Input routing

Forward left mouse events, with content-relative coordinates and `(0, 0)` at top left. Use the same units as `Resize`. The following calls belong in separate corresponding event handlers:

```csharp
bool consumedMove = ui.Input.PointerMove(mouseX, mouseY);
bool consumedDown = ui.Input.PointerDown(mouseX, mouseY);
bool consumedUp = ui.Input.PointerUp(mouseX, mouseY);
bool consumedKey = ui.Input.KeyDown(UiKey.Tab, shift: shiftHeld, repeat: isRepeat);
bool consumedRelease = ui.Input.KeyUp(UiKey.Tab);

// On native focus loss or input cancellation:
ui.Input.Cancel();
```

Map Tab, Enter, Space, and Escape to `UiKey`. Forward releases even when the pointer leaves a control. If your host requires native mouse capture to report release outside its window, enable it while `HasPointerCapture` is true. On pointer leave, forward `PointerMove(-1, -1)` to clear hover while preserving a press.

Use each return value to decide whether to pass the event to game logic. Visible UI boxes except the root body consume pointer input; empty space outside the panel goes through. Disabled buttons consume pointer input but never activate. `HasPointerCapture` and `WantsKeyboard` expose routing intent.

For a game HUD, set `pointer-events: none` on the body or decorative containers and `pointer-events: auto` on buttons. Colors ending in an alpha byte (for example `#153b3bd9`) provide translucent panels; leave the body background transparent to expose the host scene. Anchor HUD groups with `position: absolute` and `top/right/bottom/left`. See the complete example in [Lantern Valley](balloon-demo.md).

Illustrated controls can use transparent PNGs through `<img>` or `background-image`. Use `object-fit: contain` for icons and `-guishark-background-slice` plus `-guishark-background-slice-width` for scalable panel/button artwork. Source slices are bitmap pixels; destination widths are logical UI pixels. Hover/pressed/disabled classes can tint artwork using `-guishark-image-tint`. No host-side texture drawing is required. See [illustrated controls](css-subset.md#illustrated-controls) for the complete syntax.

To put a colored backing inside an ornate frame, use `background-color` with `-guishark-background-inset`. The fill moves inward; the PNG stays at the full element bounds. Tune the inset and radius against the artwork's inner edge. This prevents a rectangular color fill from covering transparent outer leaves or corners.

For artwork that should keep its proportions, use separate absolutely positioned `<img>` decorations with fixed logical dimensions and `object-fit: contain`. `-guishark-object-flip: horizontal/vertical/both` mirrors a corner without duplicating its asset or texture. Keep the outer decoration container unpadded and put padded text/content inside a child container, since absolute positioning uses the immediate parent's content box. Set decorations to `pointer-events: none`. Lantern Valley's [menu markup](../src/demos/GuiShark.Balloon/Assets/menu.html) demonstrates this arrangement; its small HUD uses simple CSS borders rather than scaling down the same frame.

A click requires pressing and releasing over the same enabled button. Dragging out and releasing cancels it. Tab follows document order and skips disabled/hidden controls. Enter/Space activate on release; repeated key-down events do not trigger callbacks. UI focus is distinct from native window focus; call `Cancel()` on native focus loss.

## Resize and DPI

Call `Resize` when logical content dimensions change. Pass actual framebuffer pixel dimensions to `Render`. For example, use `Resize(1000, 800)` and `Render(2000, 1600)` on a 2x display. Divide mouse coordinates by the host's scale if they arrive in framebuffer pixels. Text is rasterized at framebuffer scale.

The renderer covers the entire currently bound framebuffer starting at `(0, 0)`. It does not bind or clear framebuffers. To place a UI inside a smaller viewport, render into a separate framebuffer of that size and composite it yourself. Leave transform feedback inactive while drawing UI.

## Graphics state

The backend saves/restores the state it changes: program, VAO, viewport, scissor box, active texture unit, unit-0 2D texture and sampler, pixel-unpack buffer/settings, blend factors/equations, polygon mode, color mask, and enables for blending, depth, stencil, culling, scissor, framebuffer-sRGB, rasterizer discard, and color logic operations. It does not alter host VBO contents, clear values, framebuffer bindings, draw/read buffers, depth masks, or stencil functions.

Textures use premultiplied alpha with `ONE, ONE_MINUS_SRC_ALPHA` blending. UI colors are authored in display color space; framebuffer-sRGB is disabled while drawing UI. Full linear-light color management and group-opacity compositing are not implemented. Draw UI after the host scene. The demo's `Backdrop` is a separate GL renderer demonstrating that sequence.

## Mutations and assets

Use `GetElement(id).Text`, `.Disabled`, and `.SetClass(name, enabled)` to change state. Subscribe to `.Clicked` for callbacks. Mutations invalidate styles/layout, recalculated on the next update. Structure is fixed after loading; reload HTML to replace it. This version targets small menus, not thousands of live nodes.

Implement `IAssetSource` for embedded resources or game asset packs. `DirectoryAssetSource` resolves files beneath a local directory. CSS links and PNG paths are relative to that root, not to the CSS file's directory. No scripts, event-handler attributes, or remote fetches execute. Use trusted application assets; this is not a sandbox for arbitrary web content.

Fonts can be host-supplied or loaded from local CSS `@font-face` declarations. Use `new FontBook(document, "Your Family")` for the CSS-only path, or call `fonts.Load(document)` before explicit layout when combining CSS families with host defaults. The compatible renderer constructor loads declarations and uses pixel-aligned Skia + HarfBuzz by default. Named families inherit through `font-family`. Each backend provides its own layout metrics; keep a separate backend instance per renderer, but share the font book. See [text backends and CSS examples](text-rendering.md).

Text uses HarfBuzz single-run shaping and simple label word wrapping. Editable fields preserve graphemes, support selection/undo and accept IME preedit. Full bidi editing and font fallback remain future work. Skia labels are capped at 128 cached entries; stale GPU text images are released. See [multilingual host integration](multilingual-input.md). Image textures live until renderer disposal. Images/text must fit the device's maximum texture size.

See the complete adapter in [DemoWindow.cs](../src/demos/GuiShark.Demo/DemoWindow.cs) and the separate application callbacks in [DemoController.cs](../src/demos/GuiShark.Demo/DemoController.cs).
