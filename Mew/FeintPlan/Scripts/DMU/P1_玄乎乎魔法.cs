using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.DMU
{
	[FeintNode("玄乎乎魔法#1", "火+激光 - 0:37", "P1", 0u)]
	[FeintNode("玄乎乎魔法#3", "火 - 3:06", "P1", 0u)]
	public class P1_玄乎乎魔法 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			switch (serialId)
			{
			case 1u:
				return ShouldTriggerFirst(nodeId, condParams);
			case 3u:
				return condParams is EnemyCastSpellCondParams enemyCastSpellCondParams && enemyCastSpellCondParams.SpellId == 47764;
			default:
				return false;
			}
		}

		private static bool ShouldTriggerFirst(uint nodeId, IMewFeintEvent? condParams)
		{
			if (FeintHelper.IsReady(nodeId))
			{
				return true;
			}
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams && enemyCastSpellCondParams.SpellId == 47764)
			{
				FeintHelper.SetDelay(nodeId, 3000);
			}
			return false;
		}
	}
}
