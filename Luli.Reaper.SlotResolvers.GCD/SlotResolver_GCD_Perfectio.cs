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

public sealed class PerfectioGcd : Gcd
{
    public override CheckResult Check()
    {
        if (!R.Ready(R.Perfectio)) return No;
        if (R.Has(2587u) || R.Has(3858u)) return No;
        if (!R.Qt("完人")) return No;
        return R.Has(3860u) ? Ok : No;
    }
    public override PAction GetAction() => R.A(R.Perfectio);
}
