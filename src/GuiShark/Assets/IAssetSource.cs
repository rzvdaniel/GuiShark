namespace GuiShark;

/// <summary>Resolves local document assets. No implicit network requests.</summary>
public interface IAssetSource
{
    string ReadText(string relativePath);
    Stream Open(string relativePath);
}
