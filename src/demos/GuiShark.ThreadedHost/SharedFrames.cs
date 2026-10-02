namespace GuiShark.ThreadedHost;

// Texture names are shared between contexts; framebuffer objects remain context-local.
internal sealed class SharedFrames
{
    private readonly object gate = new();
    private readonly int[] textures = new int[3];
    private AppSize size;
    private int published = -1;
    private int displayed = -1;
    private int writing = -1;

    public void SetTexture(int slot, int texture, AppSize size)
    {
        lock (gate)
        {
            textures[slot] = texture;
            this.size = size;
        }
    }

    public int AcquireWriteSlot()
    {
        lock (gate)
        {
            for (var slot = 0; slot < textures.Length; slot++)
            {
                if (textures[slot] == 0 || slot == published || slot == displayed || slot == writing) continue;
                writing = slot;
                return slot;
            }
            return -1;
        }
    }

    public void Publish(int slot)
    {
        lock (gate)
        {
            if (slot != writing) throw new InvalidOperationException("Only the writer can publish a frame.");
            published = slot;
            writing = -1;
        }
    }

    public bool TryUseLatest(AppSize desired, Action<int> draw)
    {
        lock (gate)
        {
            if (published < 0 || size != desired) return false;
            displayed = published;
            draw(textures[displayed]);
            return true;
        }
    }

    public void Retire(Action deleteTextures)
    {
        lock (gate)
        {
            published = -1;
            displayed = -1;
            writing = -1;
            deleteTextures();
            Array.Clear(textures);
            size = default;
        }
    }
}
