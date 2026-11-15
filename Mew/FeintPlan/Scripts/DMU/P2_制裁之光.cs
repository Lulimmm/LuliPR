using System;
using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.DMU
{
	[FeintNode("制裁之光#1", "aoe - 5:41", "P2", 0u)]
	public class P2_制裁之光 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint id = enemyCastSpellCondParams.SpellId;
				return MemoryExtensions.Contains(DMUData.读条.制裁之光, id);
			}
			return false;
		}
	}
}
