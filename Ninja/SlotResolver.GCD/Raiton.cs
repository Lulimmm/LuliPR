using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Extension;
using AEAssist.Helper;
using Ninja.PR.Utilities.Data;
using Ninja.PR.Utilities.Settings;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.GCD
{
	public class Raiton : ISlotResolver
	{
		public int Check()
		{
			if (!Qt.Get("雷遁"))
			{
				return -110;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -2;
			}
			if (2259u.GetSpell().Charges < 1f || 2259u.RecentlyUsed(2120))
			{
				return -3;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(497u) || 2264u.RecentlyUsed(2120))
			{
				return -4;
			}
			if (NinjaSettings.Instance.UseRaiton)
			{
				return 6;
			}
			if (Qt.Get("倾斜资源"))
			{
				return 6;
			}
			if (36958u.GetSpell().Cooldown.TotalMilliseconds < 19500.0 && !((IBattleChara)(object)Core.Me).HasAura(3848u))
			{
				return -6;
			}
			if (((IGameObject)(object)Core.Me).Distance((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()) > (float)SettingMgr.GetSetting<GeneralSettings>().AttackRange && NinjaSettings.Instance.RaitonWhenOutRange)
			{
				return 1;
			}
			if (Qt.Get("斩杀") && ((ICharacter)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()).CurrentHpPercent() * 100f < NinjaSettings.Instance.TTK)
			{
				return 2;
			}
			if (((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u))
			{
				return 2;
			}
			if (((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3849u) && 2259u.GetSpell().Charges > 1f && NinjaSettings.Instance.雷顿背刺)
			{
				if (Core.Resolve<JobApi_Ninja>().Ninki == 100 && ((IBattleChara)(object)Core.Me).HasAura(2723u))
				{
					return 3;
				}
				if (((IBattleChara)(object)Core.Me).HasAura(2723u))
				{
					return 31;
				}
				if (36958u.GetSpell().Cooldown.TotalMilliseconds < 2500.0)
				{
					return 4;
				}
				if (16493u.GetSpell().Cooldown.TotalMilliseconds < 2500.0)
				{
					return 5;
				}
			}
			return -1;
		}
		public void Build(Slot slot)
		{
			2259u.GetSpell().DontUseGcdOpt = true;
			18806u.GetSpell().DontUseGcdOpt = true;
			slot.Add(2259u.GetSpell());
			slot.Add(18806u.GetSpell());
			slot.Add(2267u.GetSpell());
			NinjaSettings.Instance.UseRaiton = false;
			NinjaSettings.Instance.RaitonWhenOutRange = false;
		}
	}
}
