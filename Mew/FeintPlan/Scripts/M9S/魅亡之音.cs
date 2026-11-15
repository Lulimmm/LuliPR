using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M9S
{
	[FeintNode("魅亡之音#1", "aoe - 0:10", "", 0u)]
	[FeintNode("魅亡之音#2", "aoe - 5:05", "", 0u)]
	[FeintNode("魅亡之音#3", "aoe - 5:23", "", 0u)]
	public class 魅亡之音 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 45956;
			}
			return false;
		}
	}
}
