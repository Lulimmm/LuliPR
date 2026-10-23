using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.GCD
{
	public class TCJAction : ISlotResolver
	{
		public int Check()
		{
			if (7403u.RecentlyUsed(2120) || ((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return 1;
			}
			return -1;
		}
		public Spell GetSpell()
		{
			switch (Core.Resolve<MemApiSpellCastSuccess>().LastGcd)
			{
			case 18877u:
				return 18881u.GetSpell();
			case 18873u:
				return 18877u.GetSpell();
			default:
				return 18873u.GetSpell();
			}
		}
		public void Build(Slot slot)
		{
			GetSpell().DontUseGcdOpt = true;
			Spell spell = GetSpell();
			if (spell != null)
			{
				slot.Add(spell);
			}
		}
	}
}
