using Gui.Shark.Dom.Interfaces;

namespace Gui.Shark.Dom.Factories
{
    interface IElementFactory<T>
        where T : AngleSharp.Dom.IElement
    {
        IElement Create(AngleSharp.Dom.IElement htmlElement, IElement parent);
    }
}
