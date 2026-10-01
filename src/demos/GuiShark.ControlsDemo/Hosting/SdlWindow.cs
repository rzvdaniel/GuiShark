using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Silk.NET.Maths;
using Silk.NET.SDL;

namespace GuiShark.ControlsDemo;

/// <summary>Owns the SDL window and GL context on the calling thread.</summary>
internal sealed unsafe class SdlWindow : IDisposable, IBindingsContext
{
    public Sdl Api { get; } = Sdl.GetApi();
    private readonly Window* window;
    private readonly void* context;
    private bool disposed;
    public bool Running { get; set; } = true;
    public Vector2D<int> ClientSize
    {
        get { int width = 0, height = 0; Api.GetWindowSize(window, ref width, ref height); return new(width, height); }
    }
    public Vector2D<int> FramebufferSize
    {
        get { int width = 0, height = 0; Api.GLGetDrawableSize(window, ref width, ref height); return new(width, height); }
    }
    public SdlWindow()
    {
        try
        {
            Check(Api.Init(Sdl.InitVideo));
            Check(Api.GLSetAttribute(GLattr.ContextMajorVersion, 3));
            Check(Api.GLSetAttribute(GLattr.ContextMinorVersion, 3));
            Check(Api.GLSetAttribute(GLattr.ContextProfileMask, 1));
            Check(Api.GLSetAttribute(GLattr.ContextFlags, 2));
            window = Api.CreateWindow("GuiShark - Controls Gallery", Sdl.WindowposCentered, Sdl.WindowposCentered,
                1120, 880, (uint)(WindowFlags.Opengl | WindowFlags.Resizable | WindowFlags.AllowHighdpi));
            if (window == null) throw new InvalidOperationException(Api.GetErrorS());
            Api.SetWindowMinimumSize(window, 1120, 880);
            context = Api.GLCreateContext(window);
            if (context == null) throw new InvalidOperationException(Api.GetErrorS());
            Check(Api.GLMakeCurrent(window, context));
            GL.LoadBindings(this);
            Api.GLSetSwapInterval(1);
            Api.StopTextInput();
            Api.SetHint("SDL_IME_SHOW_UI", "1");
            Api.SetHint("SDL_IME_SUPPORT_EXTENDED_TEXT", "1");
        }
        catch { Dispose(); throw; }
    }
    private void Check(int result) { if (result < 0) throw new InvalidOperationException(Api.GetErrorS()); }
    public IntPtr GetProcAddress(string procName) => (IntPtr)Api.GLGetProcAddress(procName);
    public void SwapBuffers() => Api.GLSwapWindow(window);
    public void Dispose()
    {
        if (disposed) return;
        Api.StopTextInput();
        if (context != null) Api.GLDeleteContext(context);
        if (window != null) Api.DestroyWindow(window);
        Api.QuitSubSystem(Sdl.InitVideo);
        Api.Dispose();
        disposed = true;
    }
}
