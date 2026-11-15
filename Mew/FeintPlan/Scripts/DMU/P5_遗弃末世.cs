using System;
using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.DMU
{
	[FeintNode("遗弃末世#1", "aoe+分摊 - 17:44", "P5", 0u)]
	[FeintNode("遗弃末世#2", "aoe+分摊 - 17:53", "P5", 0u)]
	[FeintNode("遗弃末世#3", "aoe+分摊 - 18:01", "P5", 0u)]
	[FeintNode("遗弃末世#4", "aoe+分摊 - 18:09", "P5", 0u)]
	public class P5_遗弃末世 : IFeintNode
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
