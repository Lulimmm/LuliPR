using System;
using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M9S
{
	[FeintNode("血锁牢狱#1", "单人踩塔 - 6:40", "", 0u)]
	[FeintNode("血锁牢狱#2", "单人踩塔 - 7:03", "", 0u)]
	[FeintNode("血锁牢狱#3", "分组踩塔 - 7:33", "", 0u)]
	public class 血锁牢狱 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint id = enemyCastSpellCondParams.SpellId;
				return MemoryExtensions.Contains(M9SData.读条.血锁牢狱, id);
			}
			return false;
		}
	}
}
