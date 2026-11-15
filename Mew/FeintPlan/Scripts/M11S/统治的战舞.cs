using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M11S
{
	[FeintNode("统治的战舞#1", "7连aoe+剑痕 - 2:21", "", 0u)]
	public class 统治的战舞 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 46111;
			}
			return false;
		}
	}
}
