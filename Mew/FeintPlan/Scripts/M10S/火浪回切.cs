using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M10S
{
	[FeintNode("火浪回切#1", "330°可引导扇形 - 1:03", "", 0u)]
	public class 火浪回切 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (FeintHelper.IsReady(nodeId))
			{
				return true;
			}
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams && enemyCastSpellCondParams.SpellId == 46537)
			{
				float totalCastTimeInSec = enemyCastSpellCondParams.TotalCastTimeInSec;
				FeintHelper.SetDelay(nodeId, FeintHelper.CalcDelayMs(totalCastTimeInSec));
			}
			return false;
		}
	}
}
