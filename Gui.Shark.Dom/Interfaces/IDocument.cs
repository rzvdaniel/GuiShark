namespace Gui.Shark.Dom.Interfaces
{
    public interface IDocument
    {
        #region Properties

        TElement Body { get; set; }

        #endregion

        #region Methods

        void Parse(string html);
        void Render();

        #endregion
    }
}