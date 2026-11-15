using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M11S
{
	[FeintNode("铸兵突袭#1", "44分摊/8方分散/8人分摊 - 0:49", "", 0u)]
	[FeintNode("铸兵突袭#2", "44分摊/8方分散/8人分摊 - 1:35", "", 0u)]
	public class 铸兵突袭 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (FeintHelper.IsReady(nodeId))
			{
				return true;
			}
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams && enemyCastSpellCondParams.SpellId == 46103)
			{
				float totalCastTimeInSec = enemyCastSpellCondParams.TotalCastTimeInSec;
				FeintHelper.SetDelay(nodeId, FeintHelper.CalcDelayMs(totalCastTimeInSec));
			}
			return false;
		}
	}
}
