using System;
using System.IO;
using System.Text.Json;

namespace Ninja.PR;

internal sealed class NinjaSettings
{
    private static readonly string PathName = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XIVLauncherCN", "pluginConfigs", "PromeRotation", "ACR", "Cino", "ninja.json");

    public static NinjaSettings Instance { get; } = Load();
    public string Opener = "常规斩击背刺起手";
    public bool OpenerBurst;
    public bool TrickAttackDelayUntilNextMug;
    public bool P5PerfectBunshin;
    public bool RaitonBackstab;
    public bool RaitonWhenOutOfRange;
    public bool PhantomOnlyForRange;
    public bool ForceRaiton;
    public float TTK = 1.5f;

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
            File.WriteAllText(PathName, JsonSerializer.Serialize(this, new JsonSerializerOptions { IncludeFields = true }));
        }
        catch { }
    }

    private static NinjaSettings Load()
    {
        try
        {
            if (File.Exists(PathName))
                return JsonSerializer.Deserialize<NinjaSettings>(File.ReadAllText(PathName), new JsonSerializerOptions { IncludeFields = true }) ?? new();

            // Migrate the old AE settings file when this is the first PR load.
            var legacyPath = Path.Combine(Path.GetDirectoryName(PathName)!, "NinjaSettings", "Ninja.json");
            if (File.Exists(legacyPath))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(legacyPath));
                var settings = new NinjaSettings();
                if (document.RootElement.TryGetProperty("OpenerBurst", out var burst))
                    settings.OpenerBurst = burst.GetBoolean();
                if (document.RootElement.TryGetProperty("ChooseOpener", out var opener))
                {
                    settings.Opener = opener.GetString() switch
                    {
                        "Raiton" => "常规斩击背刺起手",
                        "Tudun" => "ED起手",
                        "None" => "无",
                        _ => settings.Opener
                    };
                }
                settings.Save();
                return settings;
            }
        }
        catch { }
        return new();
    }
}
