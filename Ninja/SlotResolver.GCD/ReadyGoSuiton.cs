using AEAssist.Extension;
using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Ninja.PR.Utilities.Data;

namespace Ninja.PR.SlotResolver.GCD
{
	public class ReadyGoSuiton : ISlotResolver
	{
		public static Spell 变化技能(uint id)
		{
			return Core.Resolve<MemApiSpell>().CheckActionChange(id).GetSpell();
		}

		public int Check()
		{
			if (Qt.Get("预备水遁"))
			{
				return 1;
			}
			return -1;
		}

		private Spell GetSuitonSpell()
		{
			if (变化技能(2260u).Id == 2271)
			{
				return 2271u.GetSpell();
			}
			return null;
		}

		public void Build(Slot slot)
		{
			Spell suitonSpell = GetSuitonSpell();
			if (suitonSpell != null)
			{
				slot.Add(suitonSpell);
				return;
			}
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
