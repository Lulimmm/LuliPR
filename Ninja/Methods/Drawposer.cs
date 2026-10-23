using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.CombatRoutine.View;
using AEAssist.Extension;
using AEAssist.Helper;
using Ninja.PR.SlotResolver.GCD;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.Methods
{
	public class Drawposer
	{
		public static void DrawPosHelper()
		{
			if (((IBattleChara)(object)Core.Me).GetCurrTarget() == null || !((IGameObject)((IBattleChara)(object)Core.Me).GetCurrTarget()).IsValid())
			{
				return;
			}
			int intValue = GetGCDPercentage();
			Spell nextGCD = new BaseCombo().Spellid();
			if (((IBattleChara)(object)Core.Me).GetCurrTarget() != null)
			{
				if (Core.Resolve<JobApi_Ninja>().Kazematoi == 0 && nextGCD == 3563u.GetSpell())
				{
					MeleePosHelper.Draw(MeleePosHelper.Pos.Flank, intValue);
				}
				else if (Core.Resolve<JobApi_Ninja>().Kazematoi > 3 && nextGCD == 2255u.GetSpell())
				{
					MeleePosHelper.Draw(MeleePosHelper.Pos.Behind, intValue);
				}
			}
		}

		private static int GetGCDPercentage()
		{
			float gcdCooldown = GCDHelper.GetGCDCooldown();
			float gcdDuration = GCDHelper.GetGCDDuration();
			if (gcdDuration <= 0f)
			{
				return 0;
			}
			float result = gcdCooldown / gcdDuration;
			return 100 - (int)(result * 100f);
		}
	}
}
