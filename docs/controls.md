# Controls and the gallery

Run from the repository root:

```powershell
dotnet run --project src/demos/GuiShark.ControlsDemo
```

The gallery uses the same SDK rendering and input paths as an embedded game UI. Window hosting, application callbacks, and HTML/CSS assets have separate responsibilities. It includes game settings, disabled controls, an emerald skin, keyboard focus diagnostics and a bounded live event log. F5 reloads HTML/CSS and resets state; pass `src/demos/GuiShark.ControlsDemo/Assets` as a demo argument to edit source assets directly. F12 captures the actual framebuffer. `--capture` saves a screenshot after 30 frames and exits.

## Markup

```html
<div class="row">
  <input id="subtitles" type="checkbox" checked>
  <label for="subtitles">Show subtitles</label>
</div>
<input id="easy" type="radio" name="difficulty">
<input id="normal" type="radio" name="difficulty" checked>
<label for="volume">Master volume</label>
<input id="volume" type="range" min="0" max="100" step="1" value="65">
<progress id="loading" max="100" value="65"></progress>
```

Radio buttons with the same nonempty `name` are exclusive within a document. If multiple initially have `checked`, the last wins. An unnamed radio is independent. Labels use `for` to target a control, or contain a control as a child; mixed text and children still require a separate span. Disabled or hidden targets cannot activate. Progress is determinate and noninteractive.

Ranges default to min 0, max 100, step 1, and their midpoint. Progress defaults to max 1 and value 0. Values clamp to the declared bounds; sliders also snap to a step relative to min. `step="any"` disables snapping. Invalid numeric declarations fail with a clear format error. Bounds must be finite and max must exceed min; range step must be nonnegative. Attributes initialize retained state: change the C# control state afterwards.

```csharp
var volume = document.GetElement("volume").Control!;
volume.Changed += control => audio.MasterVolume = control.Value / 100;
volume.Value = 75;
document.GetElement("subtitles").Control!.Checked = false;
document.GetElement("volume").Disabled = true;
```

`Changed` fires only when a value actually changes, for user and programmatic changes. Radio deselections notify too. Progress changes also notify, so handlers that update linked controls should avoid feedback loops. Buttons retain their `Clicked` event. Range changes are immediate while dragging; there is no separate committed-change event yet. Input interactions respect disabled state; application code can still update disabled controls.

## Theme and custom CSS

```csharp
var document = HtmlLoader.Load(html, assets, UiTheme.Neutral);
```

`UiTheme.Neutral` supplies readable control backgrounds, borders, hover/pressed/focus states, radio circles and disabled opacity. The root stays transparent. The default remains `UiTheme.None` for compatibility with existing skins. Application CSS is applied after the base theme, including inline styles. There is no automatic font discovery: load fonts using the documented `@font-face` workflow before laying out the view.

Normal CSS dimensions, backgrounds, borders and colors apply to controls. `:checked` matches checkbox/radio checked state. `-guishark-accent-color` styles marks, slider fill/thumb and progress fill; it is an element property, not inherited. This first renderer uses a solid square checkbox mark and a round radio dot. See the gallery CSS for a complete skin example.

## Input integration

Forward logical-pixel mouse coordinates to `view.Input.PointerMove`, `PointerDown`, and `PointerUp`, as with buttons. Range dragging retains capture outside its bounds; use `HasPointerCapture` when deciding whether the game should receive mouse movement. Call `Cancel` when the host loses focus.

Forward key press/release to the SDK:

- Tab / Shift+Tab move through enabled interactive controls.
- Enter / Space activate buttons, checkboxes and radios.
- Left / Down decrease a range; Right / Up increase it. Home / End select its bounds. `step="any"` uses one percent of the range for keyboard increments.
- Arrows move and select within a named radio group, skipping unavailable members.
- Escape clears focus/capture.

Hosts must map the new `UiKey.Left`, `Right`, `Up`, `Down`, `Home`, and `End` values. The gallery demonstrates this mapping. Progress is excluded from focus navigation. Radio groups currently keep each available member in the Tab order.

## Coverage

Release builds and a real OpenGL framebuffer capture were checked on Windows. Native mouse/keyboard interaction and Linux/macOS execution still need manual verification for these new controls. No unit or integration tests were added.

Dropdowns, scrolling containers, text editing/IME, tooltips, tabs and modal dialogs remain future work. This is a bounded HTML UI SDK, not a full browser form implementation.
