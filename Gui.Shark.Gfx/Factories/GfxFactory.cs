using Gui.Shark.Gfx.Interfaces;
using Gui.Shark.Gfx.OpenGL;
using System;

namespace Gui.Shark.Gfx.Factories
{
    public class GfxFactory
    {      
        public static T Create<T>()
        {
            var name = typeof(T).Name;

            switch (name)
            {
                case "IGfxServer":
                    return (T)(IGfxServer)new TGLServer();
                case "IGfxCanvas":
                    return (T)(IGfxCanvas)new TGLCanvas();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
