using Gui.Shark.Console.IO;
using Gui.Shark.Gfx.OpenGL;
using System;

namespace Gui.Shark.Console
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string path = args.Length > 0 ? args[0] : string.Empty;

            var htmlReader = new HtmlReader();
            var html = htmlReader.Read(path);

            var game = new TGame(); ;
            game.Create(800, 600, 0, html);
            game.Run(30.0);
        }
    }
}
