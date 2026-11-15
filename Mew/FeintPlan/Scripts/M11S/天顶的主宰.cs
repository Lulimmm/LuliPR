using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M11S
{
	[FeintNode("天顶的主宰#1", "aoe - 0:11", "", 0u)]
	[FeintNode("天顶的主宰#2", "aoe - 2:03", "", 0u)]
	[FeintNode("天顶的主宰#3", "aoe - 8:02", "", 0u)]
	[FeintNode("天顶的主宰#4", "aoe - 8:45", "", 0u)]
	[FeintNode("天顶的主宰#5", "aoe - 9:42", "", 0u)]
	public class 天顶的主宰 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 46086;
			}
			return false;
		}
	}
}
