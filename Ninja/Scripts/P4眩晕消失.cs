using AEAssist.Extension;
using System.Runtime.CompilerServices;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.CombatRoutine.Trigger.Node;

namespace Ninja.PR.Scripts
{
	public class P4眩晕消失 : ITriggerScript, IScript
	{
		public bool Check(ScriptEnv scriptEnv, ITriggerCondParams condParams)
		{
			if (condParams is RemoveStatusCondParams 眩晕2 && 眩晕2.StatusId == 4163)
			{
				return true;
			}
			return false;
		}
	}
}
