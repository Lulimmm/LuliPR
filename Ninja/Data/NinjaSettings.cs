using System;
using System.IO;
using System.Text.Json;

using Ninja.PR.Opener;

namespace Ninja.PR.Data;

internal sealed class NinjaSettings
{
    private static readonly string PathName = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XIVLauncherCN", "pluginConfigs", "PromeRotation", "ACR", "Cino", "ninja.json");

    public static NinjaSettings Instance { get; } = Load();
    public string Opener = NinjaOpener.StandardOpenerName;
    public bool OpenerBurst;
    public bool LockTrickAttackIntoMug = true;
    public bool TrickAttackDelayUntilNextMug;
    public bool P5PerfectBunshin;
    public bool RaitonBackstab;
    public bool RaitonWhenOutOfRange;
    public bool PhantomOnlyForRange;
    public bool ForceRaiton;
    public float ExtraRange = 3f;
    public float TTK = 1.5f;

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
            File.WriteAllText(PathName, JsonSerializer.Serialize(this,
                new JsonSerializerOptions { IncludeFields = true }));
        }
        catch { }
    }

    private static NinjaSettings Load()
    {
        try
        {
            if (File.Exists(PathName))
                return JsonSerializer.Deserialize<NinjaSettings>(File.ReadAllText(PathName),
                    new JsonSerializerOptions { IncludeFields = true }) ?? new();
        }
        catch { }

        return new();
    }
}
