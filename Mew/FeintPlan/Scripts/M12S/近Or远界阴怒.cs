using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("近/远界阴怒#1", "最近/最远22分摊 - 2:41", "本体", 0u)]
	public class 近Or远界阴怒 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint spellId = enemyCastSpellCondParams.SpellId;
				if (spellId - 46382 <= 1)
				{
					return true;
				}
			}
			return false;
		}
	}
}
