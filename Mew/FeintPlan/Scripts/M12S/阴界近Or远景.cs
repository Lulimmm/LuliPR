using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("阴界近/远景#1", "新β4人吃远/近引导分摊 - 3:50", "本体", 0u)]
	public class 阴界近Or远景 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint spellId = enemyCastSpellCondParams.SpellId;
				if (spellId - 46379 <= 1)
				{
					return true;
				}
			}
			return false;
		}
	}
}
