using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M10S
{
	[FeintNode("火蛇/水蛇夺浪#1", "aoe - 4:06", "", 0u)]
	public class 火蛇Or水蛇夺浪 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint spellId = enemyCastSpellCondParams.SpellId;
				if (spellId - 45953 <= 1)
				{
					return true;
				}
			}
			return false;
		}
	}
}
