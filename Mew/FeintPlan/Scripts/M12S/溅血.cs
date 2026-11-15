using System;
using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("溅血#1", "小aoe - 3:08", "门神", 0u)]
	[FeintNode("溅血#2", "小aoe - 4:49", "门神", 0u)]
	public class 溅血 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint id = enemyCastSpellCondParams.SpellId;
				return MemoryExtensions.Contains(M12SData.读条.溅血, id);
			}
			return false;
		}
	}
}
