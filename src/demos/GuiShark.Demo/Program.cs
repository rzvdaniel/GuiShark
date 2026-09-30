using GuiShark.Demo;

try
{
    var assets = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(AppContext.BaseDirectory, "Assets");
    using var window = new DemoWindow(assets);
    window.Run();
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    Environment.ExitCode = 1;
}
