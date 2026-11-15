using System;
using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.M10S
{
	[FeintNode("斗志昂扬#1", "aoe - 1:23", "", 0u)]
	[FeintNode("斗志昂扬#2", "aoe - 2:24", "", 0u)]
	[FeintNode("斗志昂扬#3", "aoe - 3:50", "", 0u)]
	[FeintNode("斗志昂扬#4", "aoe - 5:22", "", 0u)]
	[FeintNode("斗志昂扬#5", "aoe - 6:41", "", 0u)]
	[FeintNode("斗志昂扬#6", "aoe - 7:35", "", 0u)]
	[FeintNode("斗志昂扬#7", "aoe - 8:49", "", 0u)]
	[FeintNode("斗志昂扬#8", "aoe - 9:29", "", 0u)]
	[FeintNode("斗志昂扬#9", "aoe - 9:39", "", 0u)]
	public class 斗志昂扬 : IFeintNode
	{
		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
			{
				uint id = enemyCastSpellCondParams.SpellId;
				return MemoryExtensions.Contains(M10SData.读条.斗志昂扬, id);
			}
			return false;
		}
	}
}
