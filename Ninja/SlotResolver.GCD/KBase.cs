using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Ninja.PR.Utilities.Settings;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.GCD
{
	public class KBase : ISlotResolver
	{
		public int Check()
		{
			if (!NinjaSettings.Instance.雷顿背刺)
			{
				return -99;
			}
			if (((IGameObject)(object)Core.Me).Distance((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()) > (float)SettingMgr.GetSetting<GeneralSettings>().AttackRange)
			{
				return -1;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(2690u))
			{
				return -2;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -3;
			}
			if ((((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u) || 36958u.RecentlyUsed(1060)) && 2264u.GetSpell().Cooldown.TotalMilliseconds < 2120.0 && !((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3849u))
			{
				return 0;
			}
			return -4;
		}
		private Spell GetSpell()
		{
			uint Last = Core.Resolve<MemApiSpell>().GetLastComboSpellId();
			bool Flanking = Core.Resolve<MemApiTarget>().IsFlanking;
			_ = Core.Resolve<MemApiTarget>().IsBehind;
			if (!((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()).HasPositional() && Last == 2242)
			{
				if (Core.Resolve<JobApi_Ninja>().Kazematoi == 0)
				{
					return 3563u.GetSpell();
				}
				return 2255u.GetSpell();
			}
			if (Last == 2242)
			{
				if (Core.Resolve<JobApi_Ninja>().Kazematoi == 0)
				{
					return 3563u.GetSpell();
				}
				if (Core.Resolve<JobApi_Ninja>().Kazematoi <= 3 && Flanking)
				{
					return 3563u.GetSpell();
				}
				if (Core.Resolve<JobApi_Ninja>().Kazematoi != 0)
				{
					return 2255u.GetSpell();
				}
			}
			if (Last == 2240)
			{
				return 2242u.GetSpell();
			}
			return 2240u.GetSpell();
		}
		public void Build(Slot slot)
		{
			Spell spell = GetSpell();
			if (spell != null)
			{
				slot.Add(spell);
			}
		}
	}
}
