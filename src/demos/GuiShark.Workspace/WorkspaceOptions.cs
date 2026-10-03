namespace GuiShark.Workspace;

internal sealed record WorkspaceOptions(string Aurora, string Pulse, string StatePath, bool Verify, bool Capture)
{
    public static WorkspaceOptions Parse(string[] args)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        string Default(string project) => System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,
            $"../../../../{project}/bin/{configuration}/net10.0/app.json"));
        var aurora = Default("GuiShark.AuroraProcess");
        var pulse = Default("GuiShark.PulseProcess");
        var state = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GuiShark", "workspace.json");
        var verify = false;
        var capture = false;
        var index = 0;
        while (index < args.Length)
        {
            switch (args[index])
            {
                case "--app" when index + 1 < args.Length: aurora = System.IO.Path.GetFullPath(args[++index]); break;
                case "--pulse" when index + 1 < args.Length: pulse = System.IO.Path.GetFullPath(args[++index]); break;
                case "--state" when index + 1 < args.Length: state = System.IO.Path.GetFullPath(args[++index]); break;
                case "--verify": verify = true; break;
                case "--capture": capture = true; break;
                default: throw new ArgumentException($"Unknown or incomplete option: {args[index]}");
            }
            index++;
        }
        if (verify || capture) state = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"guishark-workspace-{Guid.NewGuid():N}.json");
        return new(aurora, pulse, state, verify, capture);
    }
}
