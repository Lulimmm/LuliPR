using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Extension;
using AEAssist.Helper;
using Ninja.PR.Utilities.Data;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.GCD
{
	public class FleetingRaiju : ISlotResolver
	{
		public int Check()
		{
			if (!Qt.Get("雷牙"))
			{
				return -99;
			}
			if (((IGameObject)(object)Core.Me).Distance((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()) > (float)SettingMgr.GetSetting<GeneralSettings>().AttackRange)
			{
				return -1;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -2;
			}
			if (((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u) && 2259u.GetSpell().Cooldown.TotalMilliseconds < 2500.0)
			{
				return -3;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(2690u))
			{
				return 0;
			}
			return -1;
		}
		public void Build(Slot slot)
		{
			slot.Add(25778u.GetSpell());
		}
	}
}
