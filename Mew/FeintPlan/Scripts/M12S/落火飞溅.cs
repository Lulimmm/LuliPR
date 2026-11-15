using System;
using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("落火飞溅#1", "1人BOSS超级跳 - 2:08", "本体", 0u)]
	public class 落火飞溅 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint id = enemyCastSpellCondParams.SpellId;
				return MemoryExtensions.Contains(M12SData.读条.落火飞溅, id);
			}
			return false;
		}
	}
}
