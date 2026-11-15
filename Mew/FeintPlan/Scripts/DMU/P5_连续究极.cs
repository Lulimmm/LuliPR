using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.DMU
{
	[FeintNode("连续究极#1", "多段aoe - 15:13", "P5", 0u)]
	[FeintNode("连续究极#2", "多段aoe - 16:35", "P5", 0u)]
	public class P5_连续究极 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 47936;
			}
			return false;
		}
	}
}
