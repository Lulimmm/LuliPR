using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Ninja.PR.Utilities.Data;
using Ninja.PR.Utilities.Settings;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.Ability
{
	public class _100B : ISlotResolver
	{
		public int Check()
		{
			Core.Resolve<MemApiSpell>().GetLastComboSpellId();
			if (!Qt.Get("六道"))
			{
				return -110;
			}
			if (((IGameObject)(object)Core.Me).Distance((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()) > (float)SettingMgr.GetSetting<GeneralSettings>().AttackRange)
			{
				return -2;
			}
			if (7402u.RecentlyUsed(1060) || 36960u.RecentlyUsed(1060))
			{
				return -3;
			}
			if (Core.Resolve<JobApi_Ninja>().Ninki < 50)
			{
				return -4;
			}
			if (36957u.RecentlyUsed(1060))
			{
				return -5;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -6;
			}
			if (36958u.GetSpell().Cooldown.TotalMilliseconds < 1260.0 && Qt.Get("小背刺") && ((IBattleChara)(object)Core.Me).HasAura(3848u))
			{
				return -7;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(3848u) && 36958u.GetSpell().Cooldown.TotalMilliseconds > 2120.0 && Core.Resolve<JobApi_Ninja>().Ninki >= 50 && Qt.Get("命水") && 16489u.GetSpell().Cooldown.TotalMilliseconds < 2120.0)
			{
				return 3;
			}
			if (Core.Resolve<JobApi_Ninja>().Ninki == 100)
			{
				return 5;
			}
			if (!NinjaSettings.Instance.P5PerfectBunshin && 36957u.GetSpell().Cooldown.TotalMilliseconds < 1060.0 && Core.Resolve<JobApi_Ninja>().Ninki > 60)
			{
				return 6;
			}
			return -1;
		}
		public void Build(Slot slot)
		{
			slot.Add(ChangeAction(7402u).GetSpell());
		}

		public static uint ChangeAction(uint SpellId)
		{
			return Core.Resolve<MemApiSpell>().CheckActionChange(SpellId);
		}
	}
}
