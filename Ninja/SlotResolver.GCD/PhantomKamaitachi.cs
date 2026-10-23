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
	public class PhantomKamaitachi : ISlotResolver
	{
		public int Check()
		{
			if (!Qt.Get("镰鼬"))
			{
				return -110;
			}
			if (!((IBattleChara)(object)Core.Me).HasAura(2723u))
			{
				return -1;
			}
			if (Core.Resolve<JobApi_Ninja>().Ninki > 90)
			{
				return -11;
			}
			if (((IGameObject)(object)Core.Me).Distance((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()) > (float)SettingMgr.GetSetting<GeneralSettings>().AttackRange)
			{
				return 0;
			}
			if (!NinjaSettings.Instance.LYForYuanli)
			{
				if (((IBattleChara)(object)Core.Me).HasAura(1954u) && ((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u))
				{
					return -1;
				}
				if (Qt.Get("倾斜资源"))
				{
					return 6;
				}
				if (NinjaSettings.Instance.雷顿背刺)
				{
					if (((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3849u) && !((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u) && 2267u.RecentlyUsed(2120))
					{
						return 1;
					}
				}
				else if (((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3849u) && !((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u))
				{
					return 1;
				}
				if (((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u) && 2259u.GetSpell().Charges < 0.9f)
				{
					return 2;
				}
				if (16493u.RecentlyUsed(20000) && 36958u.RecentlyUsed(18000) && !36957u.RecentlyUsed(20000) && 2259u.GetSpell().Charges < 1.7f)
				{
					return 3;
				}
				if (16493u.RecentlyUsed(22000) && 36958u.RecentlyUsed(22000) && 36957u.RecentlyUsed(24000) && 2259u.GetSpell().Charges < 1f)
				{
					return 4;
				}
			}
			return -1;
		}
		public void Build(Slot slot)
		{
			slot.Add(25774u.GetSpell());
		}
	}
}
