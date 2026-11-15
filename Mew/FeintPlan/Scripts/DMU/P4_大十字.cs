using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.DMU
{
	[FeintNode("大十字#1", "覆盖平a - 12:46", "P4", 0u)]
	public class P4_大十字 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (FeintHelper.IsReady(nodeId))
			{
				return true;
			}
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams && enemyCastSpellCondParams.SpellId == 47892)
			{
				FeintHelper.SetDelay(nodeId, 9000);
			}
			return false;
		}
	}
}
