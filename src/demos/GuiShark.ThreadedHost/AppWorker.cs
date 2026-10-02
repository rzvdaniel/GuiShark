using System.Collections.Concurrent;
using GuiShark.OpenGL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;

namespace GuiShark.ThreadedHost;

internal sealed class AppWorker : IDisposable
{
    private readonly NativeWindow window;
    private readonly string assetsPath;
    private readonly SharedFrames frames;
    private readonly AppSize initialSize;
    private readonly ConcurrentQueue<WorkerCommand> commands = new();
    private readonly Thread thread;
    private volatile bool stopping;
    private string status = "Starting";
    private int frameCount;
    private int clickCount;

    public string Status => Volatile.Read(ref status);
    public int FrameCount => Volatile.Read(ref frameCount);
    public int ClickCount => Volatile.Read(ref clickCount);

    public AppWorker(NativeWindow window, string assetsPath, SharedFrames frames, AppSize initialSize)
    {
        this.window = window;
        this.assetsPath = assetsPath;
        this.frames = frames;
        this.initialSize = initialSize;
        thread = new Thread(Run) { Name = "GuiShark app", IsBackground = true };
    }

    public void Start()
    {
        // GLFW window creation/destruction stay on the host thread. Only its GL context moves.
        thread.Start();
    }

    public void Send(WorkerCommand command) => commands.Enqueue(command);

    private void Run()
    {
        try
        {
            window.MakeCurrent();
            using var fonts = new FontBook(Path.Combine(assetsPath, "fonts/Lato-Regular.ttf"), Path.Combine(assetsPath, "fonts/Lato-Bold.ttf"));
            var assets = new DirectoryAssetSource(assetsPath);
            var document = HtmlLoader.Load(assets.ReadText("app.html"), assets);
            using var view = new UiView(document, fonts);
            using var renderer = new OpenGlUiRenderer(view, fonts);
            using var surface = new AppSurface(view, renderer, frames, initialSize);
            document.GetElement("increment").Clicked += _ =>
            {
                var clicks = Interlocked.Increment(ref clickCount);
                document.GetElement("count").Text = clicks.ToString();
                document.Root.SetClass("alternate", clicks % 2 == 1);
                document.GetElement("state").Text = $"Click {clicks} ran C# on the app thread";
            };
            Volatile.Write(ref status, "App live");
            while (!stopping)
            {
                ProcessCommands(view, surface);
                if (stopping) break;
                if (surface.Render()) Interlocked.Increment(ref frameCount);
                Thread.Sleep(16);
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Volatile.Write(ref status, "App stopped");
        }
        catch (Exception error)
        {
            Volatile.Write(ref status, $"App failed: {error.Message}");
            Console.Error.WriteLine(error);
        }
        finally
        {
            try { window.Context.MakeNoneCurrent(); }
            catch (Exception error) { Console.Error.WriteLine($"Unable to release app context: {error}"); }
        }
    }

    private void ProcessCommands(UiView view, AppSurface surface)
    {
        while (commands.TryDequeue(out var command))
        {
            if (command.Action == WorkerAction.Resize)
            {
                surface.Resize(command.Size);
                continue;
            }
            try
            {
                switch (command.Action)
                {
                    case WorkerAction.Move: view.Input.PointerMove(command.X, command.Y); break;
                    case WorkerAction.Down: view.Input.PointerDown(command.X, command.Y); break;
                    case WorkerAction.Up: view.Input.PointerUp(command.X, command.Y); break;
                    case WorkerAction.Freeze:
                        Volatile.Write(ref status, "App frozen for 5 seconds");
                        Thread.Sleep(5000);
                        Volatile.Write(ref status, "App resumed");
                        break;
                    case WorkerAction.Error: throw new InvalidOperationException("Deliberate app callback failure");
                }
            }
            catch (Exception error)
            {
                Volatile.Write(ref status, $"Handled app error: {error.Message}");
                Console.WriteLine(Status);
            }
        }
    }

    public void Dispose()
    {
        stopping = true;
        if (thread.IsAlive && !thread.Join(TimeSpan.FromSeconds(7)))
        {
            // A stuck app cannot be safely stopped or have its context destroyed.
            Console.Error.WriteLine("App thread did not stop; its context will remain until process exit.");
            return;
        }
        window.Dispose(); // GLFW window lifecycle stays on the host thread.
    }
}
