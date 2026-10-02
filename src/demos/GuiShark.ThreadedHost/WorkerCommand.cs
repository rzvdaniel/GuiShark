namespace GuiShark.ThreadedHost;

internal enum WorkerAction { Move, Down, Up, Resize, Freeze, Error }

internal readonly record struct WorkerCommand(WorkerAction Action, float X = 0, float Y = 0, AppSize Size = default);
