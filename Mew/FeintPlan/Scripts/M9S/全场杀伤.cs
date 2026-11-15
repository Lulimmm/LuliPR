using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M9S
{
	[FeintNode("全场杀伤#1", "大aoe+吸取以太 - 2:32", "", 0u)]
	[FeintNode("全场杀伤#2", "大aoe+吸取以太 - 6:10", "", 0u)]
	[FeintNode("全场杀伤#3", "大aoe+吸取以太 - 9:45", "", 0u)]
	public class 全场杀伤 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 45886;
			}
			return false;
		}
	}
}
