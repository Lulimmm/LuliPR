using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.DMU
{
	[FeintNode("终末双腕#2", "分摊死刑 - 6:17", "P2", 0u)]
	public class P2_终末双腕 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 49740;
			}
			return false;
		}
	}
}
