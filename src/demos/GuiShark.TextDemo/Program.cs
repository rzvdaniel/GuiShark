using GuiShark.TextDemo;

try
{
    using var window = new TextLabWindow(LaunchOptions.Parse(args));
    window.Run();
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    Environment.ExitCode = 1;
}
