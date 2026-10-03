using GuiShark.AuroraProcess;

try
{
    if (args.Length == 0 || args is ["--verify-standalone"])
    {
        using var window = new AuroraWindow(args.Length != 0);
        window.Run();
    }
    else if (args is ["--pipe", var pipeName] && !string.IsNullOrWhiteSpace(pipeName))
    {
        using var client = new AuroraPipeClient();
        using var window = new AuroraWindow(false, embedded: true);
        client.Run(window, pipeName);
    }
    else
        throw new ArgumentException("Use no arguments to open Aurora, or launch it through GuiShark.ProcessHost.");
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    Environment.ExitCode = 1;
}
