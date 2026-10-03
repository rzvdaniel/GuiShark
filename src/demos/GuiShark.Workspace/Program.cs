using GuiShark.Workspace;

try
{
    using var window = new WorkspaceWindow(WorkspaceOptions.Parse(args));
    window.Run();
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
