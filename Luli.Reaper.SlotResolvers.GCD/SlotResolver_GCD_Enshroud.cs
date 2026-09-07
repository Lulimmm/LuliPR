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

public sealed class EnshroudGcd : Gcd
{
    public override CheckResult Check()
    {
        if (!R.InEnshroud) return No;
        if (!R.Has(2593u)) return No;
        if (R.Has(2587u) || R.Has(3858u)) return No;
        if (R.Has(3860u)) return No;
        if (JobGaugeHelper.RPR.夜游魂 < 2 && ActionHelper.IsUnlocked(R.Communio))
        {
            if (MoveManager.IsLocalPlayerMoving) return No;
            return Ok;
        }
        return R.Near() ? Ok : No;
    }
    public override PAction GetAction()
    {
        if (JobGaugeHelper.RPR.夜游魂 < 2 && ActionHelper.IsUnlocked(R.Communio))
        {
            if (MoveManager.IsLocalPlayerMoving && !R.Qt("移动读条"))
            {
                if (R.Has(2594u) && R.Qt("收获月")) return R.A(R.HarvestMoon);
                return R.A(R.ShadowOfDeath);
            }
            return R.A(R.Communio);
        }
        if (!R.TargetHas(2586u, 1.5f)) return R.A(R.ShadowOfDeath);
        if (R.Has(2590u)) return R.A(R.VoidReaping);
        if (R.Has(2591u)) return R.A(R.CrossReaping);
        if (R.Cd(R.ArcaneCircle) is <= 4f and >= 1.3f && R.Qt("双附体") && !R.TargetHas(2586u, 20)) return R.A(R.ShadowOfDeath);
        if (R.Cd(R.ArcaneCircle) < 1.3f && R.Qt("双附体") && R.TargetHas(2586u, 20)) return R.A(R.VoidReaping);
        return R.A(R.VoidReaping);
    }
}
