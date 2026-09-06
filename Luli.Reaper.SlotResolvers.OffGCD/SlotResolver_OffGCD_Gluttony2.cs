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

public sealed class SacrificiumOgcd : Ogcd
{
    public override CheckResult Check()
    {
        if (!R.Qt("牲祭") || !R.Ready(R.Sacrificium) || ActionHelper.GetGcdRemain() < .8f || !R.Has(3857u) || !R.TargetHasAny(2586u) || R.Cd(R.ArcaneCircle) < 5 || R.Has(3860u)) return No;
        if (R.Cd(R.ArcaneCircle) > 110 && !R.Has(2599u)) return No;
        return Ok;
    }
    public override PAction GetAction() => R.A(R.Sacrificium, ActionType.OffGcd);
}
