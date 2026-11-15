using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("林德布鲁姆陨石#1", "小世界aoe - 5:46", "本体", 0u)]
	public class 林德布鲁姆陨石 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 46322;
			}
			return false;
		}
	}
}
