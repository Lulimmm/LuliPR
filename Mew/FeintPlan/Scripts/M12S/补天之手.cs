using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M12S
{
	[FeintNode("补天之手#1", "大aoe - 0:16", "门神", 0u)]
	[FeintNode("补天之手#2", "大aoe - 1:49", "门神", 0u)]
	[FeintNode("补天之手#3", "大aoe - 4:02", "门神", 0u)]
	public class 补天之手 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				return enemyCastSpellCondParams.SpellId == 46295;
			}
			return false;
		}
	}
}
