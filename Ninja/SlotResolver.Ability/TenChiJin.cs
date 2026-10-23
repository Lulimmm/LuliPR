using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.Extension;
using AEAssist.Helper;
using Ninja.PR.Utilities.Data;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.Ability
{
	public class TenChiJin : ISlotResolver
	{
		public int Check()
		{
			if (!Qt.Get("天地人"))
			{
				return -110;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(497u))
			{
				return -2;
			}
			if (7403u.GetSpell().Cooldown.TotalMilliseconds > 0.0 || !7403u.IsUnlock())
			{
				return -3;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(3848u))
			{
				return -4;
			}
			if (36957u.GetSpell().Cooldown.TotalMilliseconds < 2120.0)
			{
				return -5;
			}
			if (2264u.GetSpell().Cooldown.TotalMilliseconds < 2120.0)
			{
				return -6;
			}
			if (!((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3849u))
			{
				return -7;
			}
			if (2259u.GetSpell().Cooldown.TotalMilliseconds < 4000.0)
			{
				return -3;
			}
			return 0;
		}
		public void Build(Slot slot)
		{
			slot.Add(7403u.GetSpell());
		}
	}
}
