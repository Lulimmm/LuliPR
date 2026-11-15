using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M9S
{
	[FeintNode("贪欲无厌#1", "小aoe - 4:30", "", 0u)]
	[FeintNode("贪欲无厌#2", "小aoe - 9:30", "", 0u)]
	public class 贪欲无厌 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 45892;
			}
			return false;
		}
	}
}
