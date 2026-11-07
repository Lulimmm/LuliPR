using Dalamud.Interface.Textures.TextureWraps;

namespace ErosUI;

// Own a separate reference to one successfully drawn frame. Never retain the
// frame-scoped wrap returned by TryGetWrap itself across UI frames.
internal sealed class PetFrameFallback : IDisposable
{
    private string? frameName;
    internal IDalamudTextureWrap? Texture { get; private set; }

    internal void Remember(string name, IDalamudTextureWrap texture)
    {
        if (frameName == name && Texture != null) return;

        var owned = texture.CreateWrapSharingLowLevelResource();
        var previous = Texture;
        Texture = owned;
        frameName = name;
        previous?.Dispose();
    }

    public void Dispose()
    {
        var previous = Texture;
        Texture = null;
        frameName = null;
        previous?.Dispose();
    }
}
