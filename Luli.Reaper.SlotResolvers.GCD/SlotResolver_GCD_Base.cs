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

public sealed class BaseGcd : Gcd
{
    public override CheckResult Check() => R.Qt("基础连") && R.Ready(R.Slice) && R.Near() && !R.Has(2587u) && !R.Has(3858u) && !R.Recently(R.BloodStalk, 1500) && !R.Recently(R.Gluttony, 1500) ? Ok : No;
    public override PAction GetAction()
    {
        var id = ActionHelper.GetComboLeftTime() <= 0 ? R.Slice : ActionHelper.GetLastComboID() == R.Slice ? R.WaxingSlice : ActionHelper.GetLastComboID() == R.WaxingSlice ? R.InfernalSlice : R.Slice;
        return R.A(id);
    }
}
