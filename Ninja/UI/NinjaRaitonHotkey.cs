using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.UI.HotKey;

using Ninja.PR.Data;
using Ninja.PR.Helper;

namespace Ninja.PR.UI;

// Queue one complete normal Raiton mudra sequence from the hotkey panel.
// The sequence policy keeps the normal GCD resolver from inserting another
// action between Ten, Chi, and Raiton, while the high-priority queue lets the
// manual request take precedence over a normal rotation decision.
internal sealed class NinjaRaitonHotkey : IHotkeyLogic
{
    // A complete Ten -> Chi -> Raiton group occupies several future action
    // slots. Do not allow repeated clicks to reserve overlapping groups.
    private const long RequestCooldownMs = 5000;
    private static long lastQueuedAt = long.MinValue;

    public bool IsActive()
    {
        var me = Core.Me;
        var target = Core.Target;
        var now = Environment.TickCount64;

        return me != null
            && target?.CanUseAttackActionOn() == true
            && !NinjaCombatState.IsTcjSequenceActive
            && !NinjaSequencePolicy.IsActive
            && (lastQueuedAt == long.MinValue
                || now - lastQueuedAt >= RequestCooldownMs)
            && ActionHelper.IsUnlocked(NinjaActions.Ten)
            && !NinjaCombatState.Recently(NinjaActions.Raiton, 2120)
            && !NinjaCombatState.Recently(NinjaActions.Kassatsu, 2120)
            && !me.HasStatus(NinjaActions.MudraStatus)
            && !me.HasStatus(NinjaActions.KassatsuStatus);
    }

    public void OnClick()
    {
        if (!IsActive()) return;

        // No GCD-remain check is intentional: Enqueue places the whole group
        // behind the currently executing action and lets ActionUpdater submit
        // it at the next legal GCD boundary.
        NinjaSequencePolicy.Begin(NinjaActions.Raiton);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            new(NinjaActions.Ten, ActionType.Gcd, ActionTargetType.Self)
            {
                RequiresVerification = true
            },
            new(NinjaActions.ChiCombo, ActionType.Gcd, ActionTargetType.Self)
            {
                RequiresVerification = true
            },
            new(NinjaActions.Raiton, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            }
        }, isHighPriority: true);
        lastQueuedAt = Environment.TickCount64;
    }
}
