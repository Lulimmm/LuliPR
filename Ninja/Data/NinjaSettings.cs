using System;
using LuliPR.Common;
using Ninja.PR.Opener;

namespace Ninja.PR.Data;

internal sealed class NinjaSettings
{
    private readonly NinjaSettingsData data;
    public static NinjaSettings Instance { get; } = new(AcrSettingsStore.Current.Ninja);

    private NinjaSettings(NinjaSettingsData data)
    {
        this.data = data;
        if (string.IsNullOrWhiteSpace(data.Opener)) data.Opener = NinjaOpener.StandardOpenerName;
    }

    public string Opener { get => data.Opener; set => data.Opener = value; }
    public bool OpenerBurst { get => data.OpenerBurst; set => data.OpenerBurst = value; }
    public bool Positional { get => data.Positional; set => data.Positional = value; }
    public bool LockTrickAttackIntoMug { get => data.LockTrickAttackIntoMug; set => data.LockTrickAttackIntoMug = value; }
    public bool TrickAttackDelayUntilNextMug { get => data.TrickAttackDelayUntilNextMug; set => data.TrickAttackDelayUntilNextMug = value; }
    public bool P5PerfectBunshin { get => data.P5PerfectBunshin; set => data.P5PerfectBunshin = value; }
    public bool RaitonBackstab { get => data.RaitonBackstab; set => data.RaitonBackstab = value; }
    public bool RaitonWhenOutOfRange { get => data.RaitonWhenOutOfRange; set => data.RaitonWhenOutOfRange = value; }
    public bool PhantomOnlyForRange { get => data.PhantomOnlyForRange; set => data.PhantomOnlyForRange = value; }
    public bool ForceRaiton { get => data.ForceRaiton; set => data.ForceRaiton = value; }
    public float ExtraRange { get => data.ExtraRange; set => data.ExtraRange = value; }
    public float TTK { get => data.TTK; set => data.TTK = value; }

    public void Save()
    {
        AcrSettingsStore.Save();
    }
}
