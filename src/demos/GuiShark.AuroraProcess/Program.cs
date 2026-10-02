using GuiShark.AuroraProcess;

try
{
    if (args.Length == 0 || args is ["--verify-standalone"])
    {
        using var window = new AuroraWindow(args.Length != 0);
        window.Run();
    }
    else if (args is ["--pipe", var pipeName] && !string.IsNullOrWhiteSpace(pipeName))
        await new AuroraPipeClient().RunAsync(pipeName);
    else
        throw new ArgumentException("Use no arguments to open Aurora, or launch it through GuiShark.ProcessHost.");
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    Environment.ExitCode = 1;
}
