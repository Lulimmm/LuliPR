using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.Extension;
using AEAssist.Helper;
using Ninja.PR.Utilities.Data;
using Ninja.PR.Utilities.Settings;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.GCD
{
	public class KassatsuAction : ISlotResolver
	{
		public int Check()
		{
			if (!((IBattleChara)(object)Core.Me).HasAura(497u))
			{
				return -2;
			}
			if (Qt.Get("倾斜资源"))
			{
				return 6;
			}
			if (Qt.Get("斩杀") && ((ICharacter)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()).CurrentHpPercent() * 100f < NinjaSettings.Instance.TTK)
			{
				return 2;
			}
			if (((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u) || 36958u.RecentlyUsed(2120))
			{
				return 0;
			}
			if (!((IBattleChara)(object)Core.Me).HasMyAuraWithTimeleft(497u, 3000) && ((IBattleChara)(object)Core.Me).HasAura(497u))
			{
				return 1;
			}
			return -1;
		}
		public void Build(Slot slot)
		{
			18805u.GetSpell().DontUseGcdOpt = true;
			18807u.GetSpell().DontUseGcdOpt = true;
			slot.Add(18805u.GetSpell());
			slot.Add(18807u.GetSpell());
			slot.Add(16492u.GetSpell());
		}
	}
}
