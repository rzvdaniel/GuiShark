namespace GuiShark.OpenGL;

/// <summary>Image placement and frame slicing, independent of document traversal.</summary>
internal sealed class ImagePainter(QuadPainter painter)
{
    public void Draw(UiRect bounds, GpuTexture texture, ImageFit fit, UiColor tint, float opacity, float radius,
        ImageFlip flip = ImageFlip.None)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var source = new UiRect(0, 0, 1, 1);
        if (fit == ImageFit.Contain)
        {
            var scale = Math.Min(bounds.Width / texture.Width, bounds.Height / texture.Height);
            var width = texture.Width * scale;
            var height = texture.Height * scale;
            bounds = new(bounds.X + (bounds.Width - width) / 2, bounds.Y + (bounds.Height - height) / 2, width, height);
        }
        else if (fit == ImageFit.Cover)
        {
            var scale = Math.Max(bounds.Width / texture.Width, bounds.Height / texture.Height);
            var width = bounds.Width / (texture.Width * scale);
            var height = bounds.Height / (texture.Height * scale);
            source = new((1 - width) / 2, (1 - height) / 2, width, height);
        }
        if ((flip & ImageFlip.Horizontal) != 0) source = source with { X = source.Right, Width = -source.Width };
        if ((flip & ImageFlip.Vertical) != 0) source = source with { Y = source.Bottom, Height = -source.Height };
        painter.Texture(bounds, texture, tint, opacity, radius, source);
    }

    public void Frame(UiRect bounds, GpuTexture texture, Insets slice, Insets width, UiColor tint, float opacity)
    {
        var sx = Cuts(texture.Width, slice.Left, slice.Right);
        var sy = Cuts(texture.Height, slice.Top, slice.Bottom);
        var dx = Cuts(bounds.Width, width.Left, width.Right);
        var dy = Cuts(bounds.Height, width.Top, width.Bottom);
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                var source = new UiRect(sx[x] / texture.Width, sy[y] / texture.Height,
                    (sx[x + 1] - sx[x]) / texture.Width, (sy[y + 1] - sy[y]) / texture.Height);
                if (source.Width <= 0 || source.Height <= 0) continue;
                var target = new UiRect(bounds.X + dx[x], bounds.Y + dy[y], dx[x + 1] - dx[x], dy[y + 1] - dy[y]);
                painter.Texture(target, texture, tint, opacity, source: source);
            }
        }
    }

    // Reduce opposite borders proportionally when a source image or target is too small.
    private static float[] Cuts(float size, float start, float end)
    {
        var scale = start + end > size ? size / (start + end) : 1;
        return [0, start * scale, size - end * scale, size];
    }
}
