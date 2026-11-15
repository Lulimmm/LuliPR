using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("天顶猛击#1", "44分摊 - 7:21", "本体", 0u)]
	[FeintNode("天顶猛击#2", "44分摊 - 7:41", "本体", 0u)]
	public class 天顶猛击 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams && enemyCastSpellCondParams.SpellId == 46301)
			{
				return currTimeInMs > 420000;
			}
			return false;
		}
	}
}
