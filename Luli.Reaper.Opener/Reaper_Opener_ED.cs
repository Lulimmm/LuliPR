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

public sealed class EdOpener : ReaperOpener
{
    public override string OpenerName => "伊甸起手";
    public override List<PAction> InCombatSequence
    {
        get
        {
            var actions = new List<PAction> { G(R.SoulSlice) };
            AddPotion(actions);
            actions.AddRange(new[] { G(R.ShadowOfDeath), O(R.ArcaneCircle), O(R.Gluttony, ActionTargetType.Target), StanceFromTarget(), StanceFromBuff(), G(R.PlentifulHarvest) });
            return actions;
        }
    }
    public override void InitializeCountdown(CountDownHandler countdownHandler) => AddHarpeCountdown(countdownHandler, 1600);
}
