﻿using Gui.Shark.Gfx.Interfaces;
using OpenTK;

namespace Gui.Shark.Dom.Interfaces
{
    public interface IScreen
    {
        IGfxServer GfxServer { get; set; }
        IGfxCanvas Canvas { get; set; }
        IDocument Document { get; set; }

        void Create(int width, int height, Color clearColor, string htmlDocument);
        void Paint();
    }
}
