using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.PureTimeline;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using PromeRotation.Timeline;
using PromeRotation.Updaters;
using PromeRotation.UI.HotKey;

namespace Reaper.PR;

public sealed class HarpeGcd : Gcd
{
    public override CheckResult Check()
    {
        if (!R.Qt("勾刃")) return No;
        if (ReaperSpellHistory.RecentlyUsed(R.BloodStalk, 2500) && ReaperSpellHistory.RecentlyUsed(R.Gluttony, 5000)) return No;
        if (R.Has(3858u)) return No;
        if (R.Has(2587u)) return No;
        if (R.Distance < R.Range()) return No;
        if (MoveManager.IsLocalPlayerActuallyMoving) return No;
        if (R.Has(2593u)) return No;
        return Ok;
    }
    public override PAction GetAction() => R.A(R.Harpe);
}
