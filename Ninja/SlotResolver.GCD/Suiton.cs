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
	public class Suiton : ISlotResolver
	{
		public int Check()
		{
			if (!Qt.Get("水遁") || !Qt.Get("小背刺"))
			{
				return -110;
			}
			if (((IGameObject)(object)Core.Me).Distance((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()) > (float)(SettingMgr.GetSetting<GeneralSettings>().AttackRange + 17))
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
			if (2259u.GetSpell().Charges < 1f || 2259u.RecentlyUsed(2120))
			{
				return -4;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(497u) || 2264u.RecentlyUsed(2120))
			{
				return -5;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(3848u) || 2271u.RecentlyUsed(15000))
			{
				return -6;
			}
			if (36958u.GetSpell().Cooldown.TotalMilliseconds > 19500.0)
			{
				return -8;
			}
			if (NinjaSettings.Instance.TrickAttackDelayUntilNextMug && 36957u.GetSpell().Cooldown.TotalMilliseconds > 15000.0)
			{
				return -9;
			}
			if (((IGameObject)(object)Core.Me).Distance((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()) > (float)SettingMgr.GetSetting<GeneralSettings>().AttackRange)
			{
				return 1;
			}
			if (36958u.GetSpell().Cooldown.TotalMilliseconds < 15000.0)
			{
				return 2;
			}
			if (2259u.GetSpell().Cooldown.TotalMilliseconds <= 5000.0)
			{
				return 3;
			}
			return -1;
		}
		public void Build(Slot slot)
		{
			2259u.GetSpell().DontUseGcdOpt = true;
			18806u.GetSpell().DontUseGcdOpt = true;
			18807u.GetSpell().DontUseGcdOpt = true;
			slot.Add(2259u.GetSpell());
			slot.Add(18806u.GetSpell());
			slot.Add(18807u.GetSpell());
			slot.Add(2271u.GetSpell());
		}
	}
}
