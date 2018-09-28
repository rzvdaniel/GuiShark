using AngleSharp.Dom;

namespace Gui.Shark.Dom.Factories
{
    interface IElementFactory<T>
        where T : IElement
    {
        TElement Create(IElement htmlElement, TElement parent);
    }
}
