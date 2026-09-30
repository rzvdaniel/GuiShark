using GuiShark.ControlsDemo;
using var window = new GalleryWindow(args.Contains("--capture"), args.FirstOrDefault(arg => arg != "--capture"));
window.Run();
