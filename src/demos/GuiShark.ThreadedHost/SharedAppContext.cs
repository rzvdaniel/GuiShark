using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GuiShark.ThreadedHost;

internal static class SharedAppContext
{
    public static NativeWindow Create(GameWindow host)
    {
        // GLFW window creation stays on the host thread. The second context is
        // detached here and made current later by the app thread.
        var window = new NativeWindow(new NativeWindowSettings
        {
            ClientSize = new Vector2i(1, 1),
            StartVisible = false,
            SharedContext = host.Context,
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,
            Flags = ContextFlags.ForwardCompatible
        });
        window.Context.MakeNoneCurrent();
        host.MakeCurrent();
        return window;
    }
}
