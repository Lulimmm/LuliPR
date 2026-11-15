using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("境中奇狱#1", "软狂暴aoe - 8:36", "本体", 0u)]
	[FeintNode("境中奇狱#2", "软狂暴aoe - 8:52", "本体", 0u)]
	public class 境中奇狱 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 46387;
			}
			return false;
		}
	}
}
