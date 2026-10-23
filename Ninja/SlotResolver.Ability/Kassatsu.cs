using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.Extension;
using AEAssist.Helper;
using Ninja.PR.Utilities.Data;
using Ninja.PR.Utilities.Settings;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.Ability
{
	public class Kassatsu : ISlotResolver
	{
		public int Check()
		{
			if (!Qt.Get("生杀予夺"))
			{
				return -110;
			}
			if (GCDHelper.GetGCDCooldown() < 600)
			{
				return -1;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -4;
			}
			if (2264u.GetSpell().Cooldown.TotalMilliseconds > 0.0 || !2264u.IsUnlock())
			{
				return -3;
			}
			if (Qt.Get("倾斜资源"))
			{
				return 6;
			}
			if (Qt.Get("小背刺") && !((IBattleChara)(object)Core.Me).HasAura(3848u) && !((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u))
			{
				return -2;
			}
			if (NinjaSettings.Instance.雷顿背刺 && !((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u))
			{
				return -5;
			}
			return 0;
		}
		public void Build(Slot slot)
		{
			slot.Add(2264u.GetSpell());
		}
	}
}
