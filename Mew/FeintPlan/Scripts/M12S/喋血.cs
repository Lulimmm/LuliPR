using System;
using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("喋血#1", "aoe - 5:42", "门神", 0u)]
	[FeintNode("喋血#2", "aoe - 6:11", "门神", 0u)]
	[FeintNode("喋血#3", "aoe - 6:40", "门神", 0u)]
	public class 喋血 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint id = enemyCastSpellCondParams.SpellId;
				return MemoryExtensions.Contains(M12SData.读条.喋血, id);
			}
			return false;
		}
	}
}
