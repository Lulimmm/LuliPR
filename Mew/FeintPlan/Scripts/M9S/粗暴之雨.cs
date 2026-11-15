using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M9S
{
	[FeintNode("粗暴之雨#1", "多段分摊 - 0:46", "", 0u)]
	[FeintNode("粗暴之雨#2", "多段分摊 - 4:16", "", 0u)]
	[FeintNode("粗暴之雨#3", "多段分摊 - 8:18", "", 0u)]
	public class 粗暴之雨 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (FeintHelper.IsReady(nodeId))
			{
				return true;
			}
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams && enemyCastSpellCondParams.SpellId == 45917)
			{
				float totalCastTimeInSec = enemyCastSpellCondParams.TotalCastTimeInSec;
				FeintHelper.SetDelay(nodeId, FeintHelper.CalcDelayMs(totalCastTimeInSec));
			}
			return false;
		}
	}
}
