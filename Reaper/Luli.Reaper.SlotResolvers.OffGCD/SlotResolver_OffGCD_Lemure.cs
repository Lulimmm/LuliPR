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

public sealed class LemureOgcd : Ogcd
{
    public override CheckResult Check() => R.InShroud && JobGaugeHelper.RPR.虚无魂 >= 2 && R.Weave && !R.Recently(R.ArcaneCircle, 1500) && !R.Recently(R.Sacrificium, 1500) ? Ok : No;
    public override PAction GetAction()
    {
        var actionId = R.Adjust(R.LemuresSlice);
        return R.A(actionId == 0 ? R.LemuresSlice : actionId, ActionType.OffGcd, ActionTargetType.Target);
    }
}
