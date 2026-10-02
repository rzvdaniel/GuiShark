using GuiShark.ProcessHost;

try
{
    var options = HostOptions.Parse(args);
    using var window = new HostWindow(options);
    window.Run();
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    Environment.ExitCode = 1;
}
