using System;
using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.DMU
{
	[FeintNode("遗弃末世#1", "aoe - 3:55", "P2", 0u)]
	public class P2_遗弃末世 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint id = enemyCastSpellCondParams.SpellId;
				return MemoryExtensions.Contains(DMUData.读条.遗弃末世, id);
			}
			return false;
		}
	}
}
