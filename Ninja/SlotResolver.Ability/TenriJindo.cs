using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.Extension;
using AEAssist.Helper;
using Ninja.PR.Utilities.Data;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.Ability
{
	public class TenriJindo : ISlotResolver
	{
		public int Check()
		{
			if (!Qt.Get("天理人道"))
			{
				return -110;
			}
			if (GCDHelper.GetGCDCooldown() < 600)
			{
				return -1;
			}
			if (7403u.RecentlyUsed(2120) || ((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -2;
			}
			if (!((IBattleChara)(object)Core.Me).HasAura(3851u))
			{
				return -4;
			}
			return 0;
		}
		public void Build(Slot slot)
		{
			slot.Add(36961u.GetSpell());
		}
	}
}
