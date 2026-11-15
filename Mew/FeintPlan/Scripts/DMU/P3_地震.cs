using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.DMU
{
	[FeintNode("地震#1", "二运清血后平a - 9:19", "P3", 19508u)]
	public class P3_地震 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (FeintHelper.IsReady(nodeId))
			{
				return true;
			}
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams && enemyCastSpellCondParams.SpellId == 50546)
			{
				FeintHelper.SetDelay(nodeId, 7000);
			}
			return false;
		}
	}
}
