using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Ninja.PR.Utilities.Data;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.Ability
{
	public class Assassinate : ISlotResolver
	{
		public int Check()
		{
			if (!Qt.Get("梦幻三段"))
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
			if (3566u.GetSpell().Cooldown.TotalMilliseconds > 0.0)
			{
				return -3;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -4;
			}
			if (36958u.GetSpell().Cooldown.TotalMilliseconds < 2120.0 || 36957u.GetSpell().Cooldown.TotalMilliseconds < 2120.0)
			{
				return -5;
			}
			if (7403u.RecentlyUsed(2120))
			{
				return -6;
			}
			return 0;
		}
		public void Build(Slot slot)
		{
			slot.Add(ChangeAction(2246u).GetSpell());
		}

		public static uint ChangeAction(uint SpellId)
		{
			return Core.Resolve<MemApiSpell>().CheckActionChange(SpellId);
		}
	}
}
