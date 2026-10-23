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
	public class earlier4or3 : ISlotResolver
	{
		public int Check()
		{
			uint Last = Core.Resolve<MemApiSpell>().GetLastComboSpellId();
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
			if (!((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u))
			{
				return -4;
			}
			if (Last != 2242)
			{
				return -5;
			}
			if (7403u.RecentlyUsed(8000) && 25778u.RecentlyUsed(2120))
			{
				return 0;
			}
			return -1;
		}
		private Spell GetSpell()
		{
			if (Core.Resolve<JobApi_Ninja>().Kazematoi == 0)
			{
				return 3563u.GetSpell();
			}
			return 2255u.GetSpell();
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
