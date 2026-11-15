using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("境中奇梦#1", "四运开场aoe - 4:30", "本体", 0u)]
	[FeintNode("境中奇梦#2", "四运结束aoe - 7:53", "本体", 0u)]
	public class 境中奇梦 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 46345;
			}
			return false;
		}
	}
}
