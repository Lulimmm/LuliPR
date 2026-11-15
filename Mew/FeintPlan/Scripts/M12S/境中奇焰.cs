using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("境中奇焰#1", "aoe - 0:17", "本体", 0u)]
	[FeintNode("境中奇焰#2", "aoe - 3:58", "本体", 0u)]
	public class 境中奇焰 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 46376;
			}
			return false;
		}
	}
}
