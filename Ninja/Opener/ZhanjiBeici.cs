using AEAssist.Extension;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Ninja.PR.Utilities.Settings;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.Opener
{
	public class ZhanjiBeici : IOpener, ISlotSequence, IScript
	{
		public List<Action<Slot>> Sequence { get; } = new List<Action<Slot>> { Step0, Step1, Step2, Step3, Step4 };

		public Action CompeltedAction { get; set; }

		public int StartCheck()
		{
			if (((ICharacter)Core.Me).Level == 100)
			{
				return 0;
			}
			return -1;
		}

		public int StopCheck(int index)
		{
			return -1;
		}

		private static void Step0(Slot slot)
		{
			slot.Add(2264u.GetSpell());
			slot.Add(2240u.GetSpell());
			if (NinjaSettings.Instance.OpenerBurst)
			{
				slot.Add(Spell.CreatePotion());
			}
		}

		private static void Step1(Slot slot)
		{
			slot.Add(2242u.GetSpell());
			slot.Add(Core.Resolve<MemApiSpell>().CheckActionChange(2248u).GetSpell());
		}

		private static void Step2(Slot slot)
		{
			slot.Add(3563u.GetSpell());
			slot.Add(16493u.GetSpell());
		}

		private static void Step3(Slot slot)
		{
			slot.Add(25774u.GetSpell());
			slot.Add(new SlotAction(SlotAction.WaitType.WaitForSndHalfWindow, 365, Core.Resolve<MemApiSpell>().CheckActionChange(2258u).GetSpell()));
		}

		private static void Step4(Slot slot)
		{
			slot.Add(18805u.GetSpell());
			slot.Add(18807u.GetSpell());
			slot.Add(16492u.GetSpell());
			slot.Add(7403u.GetSpell());
		}

		public void InitCountDown(CountDownHandler countDownHandler)
		{
			countDownHandler.AddAction(5000, 2259u);
			countDownHandler.AddAction(4500, 18806u);
			countDownHandler.AddAction(4000, 18807u);
			countDownHandler.AddAction(650, 2271u, SpellTargetType.Target);
		}
	}
}
