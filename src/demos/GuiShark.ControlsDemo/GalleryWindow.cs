using GuiShark.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;

namespace GuiShark.ControlsDemo;

internal sealed class GalleryWindow : GameWindow
{
    private readonly string assetsPath;
    private FontBook fonts = null!;
    private UiView view = null!;
    private OpenGlUiRenderer renderer = null!;
    private GalleryController controller = null!;
    private int frames;
    private readonly GalleryOptions options;
    private bool capture;

    public GalleryWindow(GalleryOptions options) : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
    {
        ClientSize = new Vector2i(1120, 880),
        MinimumClientSize = new Vector2i(1120, 880),
        Title = "GuiShark · Controls Gallery",
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        Flags = ContextFlags.ForwardCompatible
    })
    {
        this.options = options;
        this.assetsPath = Path.GetFullPath(options.AssetsPath ?? Path.Combine(AppContext.BaseDirectory, "Assets"));
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        VSync = VSyncMode.On;
        Console.WriteLine($"OpenGL {GL.GetString(StringName.Version)} / {GL.GetString(StringName.Renderer)}");
        fonts = new(Path.Combine(assetsPath, "fonts/Lato-Regular.ttf"), Path.Combine(assetsPath, "fonts/Lato-Bold.ttf"));
        LoadUi();
        Console.WriteLine("F5 reloads HTML/CSS. F12 saves a screenshot. Tab/Shift+Tab navigate controls; Enter/Space activate; arrows adjust controls/switch tabs; wheel scrolls lists.");
    }

    private sealed class WindowClipboard(GalleryWindow window) : IUiClipboard
    {
        public string GetText() => window.ClipboardString;
        public void SetText(string text) => window.ClipboardString = text;
    }

    private void LoadUi()
    {
        // Construct first so a CSS error on reload leaves the current UI usable.
        var assets = new DirectoryAssetSource(assetsPath);
        var document = HtmlLoader.Load(assets.ReadText("index.html"), assets, UiTheme.Neutral);
        fonts.Load(document);
        var nextView = new UiView(document, fonts);
        nextView.Input.Clipboard = new WindowClipboard(this);
        nextView.Resize(ClientSize.X, ClientSize.Y);
        nextView.Update();
        var nextRenderer = new OpenGlUiRenderer(nextView, GalleryTextBackend.Create(options.TextBackend, fonts), ownsTextBackend: true);
        renderer?.Dispose();
        view?.Dispose();
        view = nextView;
        renderer = nextRenderer;
        controller = new(view);
        document.TabGroups[0].Select(document.GetElement($"tab-{options.Page}"));
        view.Update();
        if (options.SelectNotes)
        {
            var notes = document.GetElement("journal-notes");
            view.Input.Focus(notes);
            var start = notes.TextInput!.Value.IndexOf("The valley", StringComparison.Ordinal);
            var end = notes.TextInput.Value.IndexOf("Remember:", StringComparison.Ordinal) + "Remember:".Length;
            notes.TextInput.Select(start, end - start);
            view.Update();
        }
        if (options.Scrolled && options.Page == "journal")
        {
            var scroll = document.GetElement("journal-notes").Scroll;
            scroll.Offset = scroll.Maximum;
            view.Update();
        }
        else if (options.Scrolled)
        {
            document.GetElement("quest-scroll").Scroll.Offset = 380;
            document.GetElement("nested-inner").Scroll.Offset = 150;
            view.Update();
        }
        if (options.Modal == "password") document.GetElement("password-dialog").Dialog!.ShowModal();
        else if (options.Modal == "name")
        {
            document.GetElement("name-dialog").Dialog!.ShowModal();
            document.GetElement("character-name").TextInput!.SelectAll();
        }
        else if (options.Modal != null) controller.Inventory.Confirm(options.Modal);
        if (options.Dropdown != null) view.Popup.Open(document.GetElement(options.Dropdown));
        if (options.Tooltip != null)
        {
            view.Update();
            var bounds = document.GetElement(options.Tooltip).Bounds;
            view.Input.PointerMove(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Disable(EnableCap.ScissorTest);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(.035f, .055f, .09f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        controller.Update(view.Input.Focused);
        if (options.Capture && options.Tooltip != null)
        {
            view.Update();
            var bounds = view.Document.GetElement(options.Tooltip).Bounds;
            view.Input.PointerMove(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        }
        if (options.Capture && ++frames == 30) capture = true;
        renderer.Render(FramebufferSize.X, FramebufferSize.Y);
        if (capture)
        {
            capture = false;
            GuiShark.Demo.FrameCapture.Save(FramebufferSize.X, FramebufferSize.Y);
        }
        SwapBuffers();
        if (options.Capture && frames >= 30) Close();
    }

    protected override void OnResize(ResizeEventArgs args)
    {
        base.OnResize(args);
        view?.Resize(ClientSize.X, ClientSize.Y);
    }

    protected override void OnMouseMove(MouseMoveEventArgs args)
    {
        base.OnMouseMove(args);
        view?.Input.PointerMove(args.X, args.Y);
    }

    protected override void OnMouseDown(MouseButtonEventArgs args)
    {
        base.OnMouseDown(args);
        if (args.Button == MouseButton.Left) view?.Input.PointerDown(MousePosition.X, MousePosition.Y, KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift));
    }

    protected override void OnMouseUp(MouseButtonEventArgs args)
    {
        base.OnMouseUp(args);
        if (args.Button == MouseButton.Left) view?.Input.PointerUp(MousePosition.X, MousePosition.Y);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs args)
    {
        base.OnMouseWheel(args);
        view?.Input.PointerWheel(MousePosition.X, MousePosition.Y, args.OffsetY);
    }

    protected override void OnFocusedChanged(FocusedChangedEventArgs args)
    {
        base.OnFocusedChanged(args);
        if (!args.IsFocused) view?.Input.Cancel();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs args)
    {
        base.OnKeyDown(args);
        if (MapKey(args.Key) is { } key && view.Input.KeyDown(key, args.Shift, args.IsRepeat, args.Control || args.Command,
            wordNavigation: OperatingSystem.IsMacOS() ? args.Alt : args.Control)) return;
        if (view.Input.WantsKeyboard && args.Key != Keys.F5 && args.Key != Keys.F12) return;
        if (args.Key == Keys.Escape) Close();
        if (args.Key == Keys.F12) capture = true;
        if (args.Key == Keys.F5 && !args.IsRepeat)
            try { LoadUi(); } catch (Exception error) { Console.Error.WriteLine($"Reload failed: {error.Message}"); }
    }

    protected override void OnTextInput(TextInputEventArgs args)
    {
        base.OnTextInput(args);
        view?.Input.TextInput(args.AsString);
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs args)
    {
        base.OnKeyUp(args);
        if (MapKey(args.Key) is { } key) view.Input.KeyUp(key);
    }

    private static UiKey? MapKey(Keys key) => key switch
    {
        Keys.Tab => UiKey.Tab, Keys.Enter or Keys.KeyPadEnter => UiKey.Enter,
        Keys.Space => UiKey.Space, Keys.Escape => UiKey.Escape,
        Keys.Left => UiKey.Left, Keys.Right => UiKey.Right, Keys.Up => UiKey.Up, Keys.Down => UiKey.Down,
        Keys.Home => UiKey.Home, Keys.End => UiKey.End,
        Keys.Backspace => UiKey.Backspace, Keys.Delete => UiKey.Delete,
        Keys.Z => UiKey.Z, Keys.Y => UiKey.Y, Keys.A => UiKey.A, Keys.C => UiKey.C, Keys.X => UiKey.X, Keys.V => UiKey.V,
        Keys.PageUp => UiKey.PageUp, Keys.PageDown => UiKey.PageDown, _ => null
    };

    protected override void OnUnload()
    {
        renderer?.Dispose();
        view?.Dispose();
        fonts?.Dispose();
        base.OnUnload();
    }
}
