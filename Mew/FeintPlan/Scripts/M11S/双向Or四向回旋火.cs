using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M11S
{
	[FeintNode("双向/四向回旋火#1", "44分摊/22分摊 - 9:30", "", 0u)]
	public class 双向Or四向回旋火 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint spellId = enemyCastSpellCondParams.SpellId;
				if (spellId == 46170 || spellId == 47037)
				{
					return true;
				}
			}
			return false;
		}
	}
}
