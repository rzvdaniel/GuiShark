namespace GuiShark.Workspace;

internal sealed record WorkspaceOptions(string Aurora, string Pulse, string StatePath, bool Verify, bool Capture, bool SharedFrames = true, bool Benchmark = false, int Fps = 60)
{
    private static string ReadValue(string[] args, ref int index)
    {
        if (index + 1 >= args.Length) throw new ArgumentException($"Missing value for {args[index]}");
        return args[++index];
    }
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
        var sharedFrames = true;
        var benchmark = false;
        int? fps = null;
        var index = 0;
        while (index < args.Length)
        {
            switch (args[index])
            {
                case "--app": aurora = System.IO.Path.GetFullPath(ReadValue(args, ref index)); break;
                case "--pulse": pulse = System.IO.Path.GetFullPath(ReadValue(args, ref index)); break;
                case "--state": state = System.IO.Path.GetFullPath(ReadValue(args, ref index)); break;
                case "--verify": verify = true; break;
                case "--capture": capture = true; break;
                case "--png": sharedFrames = false; break;
                case "--benchmark": benchmark = true; break;
                case "--fps": fps = int.Parse(ReadValue(args, ref index)); break;
                default: throw new ArgumentException($"Unknown or incomplete option: {args[index]}");
            }
            index++;
        }
        return new WorkspaceOptions(aurora, pulse, state, verify, capture, sharedFrames, benchmark, fps ?? (sharedFrames ? 60 : 10)).Validate();
    }
    private WorkspaceOptions Validate()
    {
        if (Fps is < 1 or > 60) throw new ArgumentException("Frame rate must be between 1 and 60.");
        if (!Verify && !Capture && !Benchmark) return this;
        return this with { StatePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"guishark-workspace-{Guid.NewGuid():N}.json") };
    }
}
