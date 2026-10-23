using AEAssist.Extension;
using System.Runtime.CompilerServices;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.CombatRoutine.Trigger.Node;

namespace Ninja.PR.Scripts
{
	public class P2击退 : ITriggerScript, IScript
	{
		public bool Check(ScriptEnv scriptEnv, ITriggerCondParams condParams)
		{
			if (condParams is ReceviceAbilityEffectCondParams 击退 && 击退.ActionId == 40207)
			{
				return true;
			}
			return false;
		}
	}
}
