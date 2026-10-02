using GuiShark.ThreadedHost;

try
{
    using var window = new HostWindow(Path.Combine(AppContext.BaseDirectory, "Assets"), args.Contains("--verify"), args.Contains("--capture"));
    window.Run();
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    Environment.ExitCode = 1;
}
