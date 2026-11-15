using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M11S
{
	[FeintNode("大火#1", "陨石+分摊 - 5:00", "", 0u)]
	[FeintNode("大火#2", "陨石+分摊 - 5:10", "", 0u)]
	[FeintNode("大火#3", "陨石+分摊 - 5:20", "", 0u)]
	[FeintNode("大火#4", "陨石+分摊 - 5:30", "", 0u)]
	public class 大火 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (FeintHelper.IsReady(nodeId))
			{
				return true;
			}
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams && enemyCastSpellCondParams.SpellId == 46137)
			{
				float totalCastTimeInSec = enemyCastSpellCondParams.TotalCastTimeInSec;
				FeintHelper.SetDelay(nodeId, FeintHelper.CalcDelayMs(totalCastTimeInSec));
			}
			return false;
		}
	}
}
