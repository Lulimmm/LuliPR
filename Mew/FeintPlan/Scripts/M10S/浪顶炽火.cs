using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M10S
{
	[FeintNode("浪顶炽火#1", "4人直线距离衰减 - 0:31", "", 0u)]
	public class 浪顶炽火 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (FeintHelper.IsReady(nodeId))
			{
				return true;
			}
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams && enemyCastSpellCondParams.SpellId == 46522)
			{
				float totalCastTimeInSec = enemyCastSpellCondParams.TotalCastTimeInSec;
				FeintHelper.SetDelay(nodeId, FeintHelper.CalcDelayMs(totalCastTimeInSec - 2f));
			}
			return false;
		}
	}
}
