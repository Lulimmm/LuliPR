using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Ninja.PR.Methods;
using Ninja.PR.SlotResolver.GCD;
using Ninja.PR.Utilities.Data;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.Ability
{
	public class Bhavacakra : ISlotResolver
	{
		public int Check()
		{
			uint Last = Core.Resolve<MemApiSpell>().GetLastComboSpellId();
			if (!Qt.Get("六道"))
			{
				return -110;
			}
			if (GCDHelper.GetGCDCooldown() < 600)
			{
				return -1;
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
			if (16493u.GetSpell().Cooldown.TotalMilliseconds < 20000.0 && Qt.Get("分身"))
			{
				if (!((IBattleChara)(object)Core.Me).HasAura(1954u))
				{
					if (SlotResolverManager.GetFirstValidSlotResolver() is BaseCombo && ((Last == 2242 && Core.Resolve<JobApi_Ninja>().Ninki > 85) || Core.Resolve<JobApi_Ninja>().Ninki > 95))
					{
						return 61;
					}
					if (SlotResolverManager.GetFirstValidSlotResolver() is FleetingRaiju && Core.Resolve<JobApi_Ninja>().Ninki > 95)
					{
						return 62;
					}
				}
				return -7;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1954u) && ((Last == 2242 && Core.Resolve<JobApi_Ninja>().Ninki > 80) || (Core.Resolve<JobApi_Ninja>().Ninki > 90 && SlotResolverManager.GetFirstValidSlotResolver() is BaseCombo)))
			{
				return 3;
			}
			if ((Last == 2242 && Core.Resolve<JobApi_Ninja>().Ninki > 85) || (Core.Resolve<JobApi_Ninja>().Ninki > 95 && SlotResolverManager.GetFirstValidSlotResolver() is BaseCombo))
			{
				return 3;
			}
			if (36958u.GetSpell().Cooldown.TotalMilliseconds < 2120.0)
			{
				return -5;
			}
			if (2264u.GetSpell().Cooldown.TotalMilliseconds < 2120.0)
			{
				return -5;
			}
			if (((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u))
			{
				return 2;
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
