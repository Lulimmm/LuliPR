using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M9S
{
	[FeintNode("施虐的尖啸#1", "aoe+切换场地 - 1:00", "", 0u)]
	[FeintNode("施虐的尖啸#2", "aoe+切换场地 - 2:18", "", 0u)]
	[FeintNode("施虐的尖啸#3", "aoe+切换场地 - 4:44", "", 0u)]
	[FeintNode("施虐的尖啸#4", "aoe+切换场地 - 5:56", "", 0u)]
	public class 施虐的尖啸 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 45875;
			}
			return false;
		}
	}
}
