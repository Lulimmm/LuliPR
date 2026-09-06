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
        if (!R.Qt("勾刃") || !R.Ready(R.Harpe) || !R.Targetable || R.Near() || R.Distance > 25 || R.Has(2587u) || R.Has(3858u) || R.InShroud || MoveManager.IsLocalPlayerMoving) return No;
        return R.Recently(R.BloodStalk, 2500) && R.Recently(R.Gluttony, 5000) ? No : Ok;
    }
    public override PAction GetAction() => R.A(R.Harpe);
}
