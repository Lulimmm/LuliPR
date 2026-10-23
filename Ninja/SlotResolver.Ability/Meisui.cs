using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.Extension;
using AEAssist.Helper;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.Ability
{
	public class Meisui : ISlotResolver
	{
		public int Check()
		{
			if (GCDHelper.GetGCDCooldown() < 600)
			{
				return -1;
			}
			if (Core.Resolve<JobApi_Ninja>().Ninki >= 50)
			{
				return -2;
			}
			if (16489u.GetSpell().Cooldown.TotalMilliseconds > 0.0 || !16489u.IsUnlock())
			{
				return -3;
			}
			if (!((IBattleChara)(object)Core.Me).HasAura(3848u))
			{
				return -4;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -4;
			}
			if (!18881u.RecentlyUsed(15000) || 36958u.GetSpell().Cooldown.TotalMilliseconds < 20000.0)
			{
				return -6;
			}
			return 0;
		}
		public void Build(Slot slot)
		{
			slot.Add(16489u.GetSpell());
		}
	}
}
