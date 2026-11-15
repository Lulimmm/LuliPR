using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M11S
{
	[FeintNode("碎心踢#1", "分摊塔 - 9:56", "", 0u)]
	[FeintNode("碎心踢#2", "分摊塔 - 10:15", "", 0u)]
	[FeintNode("碎心踢#3", "分摊塔 - 10:36", "", 0u)]
	public class 碎心踢 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 46173;
			}
			return false;
		}
	}
}
