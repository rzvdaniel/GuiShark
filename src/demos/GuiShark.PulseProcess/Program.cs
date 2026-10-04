using GuiShark.ProcessHosting;
using GuiShark.PulseProcess;

try
{
    if (args.Length == 0) { using var window = new PulseWindow(); window.Run(); }
    else if (args is ["--pipe", var name])
    {
        using var client = new AppPipeClient();
        using var window = new PulseWindow(true);
        client.Run(window, name);
    }
    else throw new ArgumentException("Use no arguments or --pipe <name>.");
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
