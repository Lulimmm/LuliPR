using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M10S
{
	[FeintNode("极限火蛇/水蛇夺浪#1", "aoe - 7:50", "", 0u)]
	public class 极限火蛇Or水蛇夺浪 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint spellId = enemyCastSpellCondParams.SpellId;
				if (spellId - 46510 <= 1)
				{
					return true;
				}
			}
			return false;
		}
	}
}
