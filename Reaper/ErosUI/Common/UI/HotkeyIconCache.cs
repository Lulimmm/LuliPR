using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using ECommons.DalamudServices;
using PromeRotation.Helpers;

namespace ErosUI;

// Cache shared resources, not frame-scoped wraps. Dynamic action/item IDs are
// still evaluated by the caller every draw; a changed ID selects another entry.
internal sealed class HotkeyIconCache
{
    private readonly Dictionary<uint, uint> actionIcons = new();
    private readonly Dictionary<(uint Icon, bool Hq), IconEntry> icons = new();

    private sealed class IconEntry
    {
        public ISharedImmediateTexture? Texture;
        public long RetryAfter;
        public long? UnavailableSince;
    }

    internal IDalamudTextureWrap? GetActionIcon(uint actionId)
    {
        if (!actionIcons.TryGetValue(actionId, out var iconId))
        {
            var resolved = actionId.GetActionIconId();
            if (resolved is not > 0) return null;
            iconId = resolved.Value;
            actionIcons.Add(actionId, iconId);
        }
        return GetGameIcon(iconId, false);
    }

    internal IDalamudTextureWrap? GetGameIcon(uint iconId, bool hq)
    {
        var key = (iconId, hq);
        if (!icons.TryGetValue(key, out var entry))
            icons.Add(key, entry = new IconEntry());
        var now = Environment.TickCount64;
        if (now < entry.RetryAfter) return null;
        try
        {
            entry.Texture ??= Svc.Texture.GetFromGameIcon(new GameIconLookup(iconId, itemHq: hq));
            if (entry.Texture.TryGetWrap(out var wrap, out var error) && wrap != null)
            {
                entry.UnavailableSince = null;
                return wrap;
            }
            if (error != null)
                Retry(entry, now);
            else if (entry.UnavailableSince is not { } since)
                entry.UnavailableSince = now;
            else if (now - since >= 15_000)
                Retry(entry, now);
        }
        catch
        {
            Retry(entry, now);
        }
        return null;
    }

    internal void Invalidate(uint iconId, bool hq, uint actionId)
    {
        var now = Environment.TickCount64;
        if (icons.TryGetValue((iconId, hq), out var entry)) Retry(entry, now);
        if (actionIcons.TryGetValue(actionId, out var actionIcon)
            && icons.TryGetValue((actionIcon, false), out entry)) Retry(entry, now);
    }

    private static void Retry(IconEntry entry, long now)
    {
        // Let the provider release its failed idle task before retrying it.
        entry.Texture = null;
        entry.UnavailableSince = null;
        entry.RetryAfter = now + 5_000;
    }
}
