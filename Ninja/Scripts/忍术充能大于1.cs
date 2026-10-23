using AEAssist.Extension;
using System.Runtime.CompilerServices;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.CombatRoutine.Trigger.Node;
using AEAssist.Helper;

namespace Ninja.PR.Scripts
{
	public class 忍术充能大于1 : ITriggerScript, IScript
	{
		public bool Check(ScriptEnv scriptEnv, ITriggerCondParams condParams)
		{
			if (2259u.GetSpell().Charges > 1f)
			{
				return true;
			}
			return false;
		}
	}
}
