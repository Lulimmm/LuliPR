using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M9S
{
	[FeintNode("致命的闭幕曲#1", "大aoe - 2:51", "", 0u)]
	[FeintNode("致命的闭幕曲#2", "大aoe - 6:29", "", 0u)]
	public class 致命的闭幕曲 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 45888;
			}
			return false;
		}
	}
}
