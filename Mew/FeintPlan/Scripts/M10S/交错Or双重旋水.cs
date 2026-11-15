using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M10S
{
	[FeintNode("交错/双重旋水#1", "八方引导扇形 - 2:06", "", 0u)]
	[FeintNode("交错/双重旋水#2", "八方引导扇形 - 4:18", "", 0u)]
	[FeintNode("交错/双重旋水#3", "八方引导扇形 - 7:11", "", 0u)]
	[FeintNode("交错/双重旋水#4", "八方引导扇形 - 9:04", "", 0u)]
	public class 交错Or双重旋水 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint spellId = enemyCastSpellCondParams.SpellId;
				if (spellId == 46557 || spellId == 46560)
				{
					return true;
				}
			}
			return false;
		}
	}
}
