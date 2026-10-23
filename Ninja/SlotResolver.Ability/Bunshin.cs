using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Ninja.PR.Utilities.Data;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.Ability
{
	public class Bunshin : ISlotResolver
	{
		public int Check()
		{
			if (!Qt.Get("分身"))
			{
				return -110;
			}
			if (GCDHelper.GetGCDCooldown() < 300)
			{
				return -1;
			}
			if (16493u.GetSpell().Cooldown.TotalMilliseconds != 0.0)
			{
				return -2;
			}
			if (Core.Resolve<JobApi_Ninja>().Ninki < 50)
			{
				return -3;
			}
			if (36957u.GetSpell().Cooldown.TotalMilliseconds < 1060.0)
			{
				return -4;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -5;
			}
			return 0;
		}
		public void Build(Slot slot)
		{
			slot.Add(16493u.GetSpell());
		}

		public static uint ChangeAction(uint SpellId)
		{
			return Core.Resolve<MemApiSpell>().CheckActionChange(SpellId);
		}
	}
}
