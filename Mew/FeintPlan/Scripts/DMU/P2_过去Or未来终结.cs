using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.DMU
{
	[FeintNode("过去/未来终结#2", "第四、五轮踩塔 - 4:39", "P2", 0u)]
	public class P2_过去Or未来终结 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (FeintHelper.IsReady(nodeId))
			{
				return true;
			}
			bool flag;
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint spellId = enemyCastSpellCondParams.SpellId;
				if (spellId - 47826 <= 1)
				{
					flag = true;
					goto IL_002c;
				}
			}
			flag = false;
			goto IL_002c;
			IL_002c:
			if (flag)
			{
				FeintHelper.SetDelay(nodeId, 4000);
			}
			return false;
		}
	}
}
