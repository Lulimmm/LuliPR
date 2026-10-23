using AEAssist.Extension;
using System.Runtime.CompilerServices;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.CombatRoutine.Trigger.Node;

namespace Ninja.PR.Scripts
{
	public class buff消失 : ITriggerScript, IScript
	{
		public bool Check(ScriptEnv scriptEnv, ITriggerCondParams condParams)
		{
			if (condParams is RemoveStatusCondParams 冻结 && 冻结.StatusId == 1150)
			{
				return true;
			}
			return false;
		}
	}
}
