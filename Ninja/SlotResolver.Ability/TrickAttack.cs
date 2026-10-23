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
	public class TrickAttack : ISlotResolver
	{
		public int Check()
		{
			if (!Qt.Get("小背刺"))
			{
				return -110;
			}
			if (((IGameObject)(object)Core.Me).Distance((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()) > (float)SettingMgr.GetSetting<GeneralSettings>().AttackRange)
			{
				return -2;
			}
			if (36958u.GetSpell().Cooldown.TotalMilliseconds > 0.0 || !36958u.IsUnlock())
			{
				return -3;
			}
			if (!((IBattleChara)(object)Core.Me).HasAura(3848u))
			{
				return -4;
			}
			if (36957u.GetSpell().Cooldown.TotalMilliseconds == 0.0 && Qt.Get("大背刺"))
			{
				return -5;
			}
			if (36957u.RecentlyUsed(1360))
			{
				return -6;
			}
			if (NinjaSettings.Instance.TrickAttackDelayUntilNextMug && !((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3849u))
			{
				return -7;
			}
			if (18881u.RecentlyUsed() && GCDHelper.GetElapsedGCD() < 700)
			{
				return -1;
			}
			if (2267u.RecentlyUsed(1500) && GCDHelper.GetElapsedGCD() < 700)
			{
				return -1;
			}
			if (7403u.GetSpell().Cooldown.TotalMilliseconds < 10000.0 && GCDHelper.GetElapsedGCD() < 1450)
			{
				return -1;
			}
			return 0;
		}
		public void Build(Slot slot)
		{
			slot.Add(ChangeAction(2258u).GetSpell());
			NinjaSettings.Instance.TrickAttackDelayUntilNextMug = false;
		}

		public static uint ChangeAction(uint SpellId)
		{
			return Core.Resolve<MemApiSpell>().CheckActionChange(SpellId);
		}
	}
}
