using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("双重飞踢#1", "分摊死刑 - 1:11", "本体", 0u)]
	[FeintNode("双重飞踢#2", "分摊死刑 - 4:07", "本体", 0u)]
	[FeintNode("双重飞踢#3", "分摊死刑 - 8:01", "本体", 0u)]
	public class 双重飞踢 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 46368;
			}
			return false;
		}
	}
}
