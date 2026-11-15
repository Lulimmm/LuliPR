using System;
using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.DMU
{
	[FeintNode("制裁之光#1", "aoe - 1:02", "P1", 0u)]
	[FeintNode("制裁之光#2", "aoe - 2:12", "P1", 0u)]
	public class P1_制裁之光 : IFeintNode
	{
		private int 岩石弹Count;

		private int 上次岩石弹时间;

		public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			switch (serialId)
			{
			case 1u:
			{
				int result;
				if (condParams is EnemyCastSpellCondParams enemyCastSpellCondParams)
				{
					uint id = enemyCastSpellCondParams.SpellId;
					result = (MemoryExtensions.Contains(DMUData.读条.制裁之光, id) ? 1 : 0);
				}
				else
				{
					result = 0;
				}
				return (byte)result != 0;
			}
			case 2u:
				return ShouldTriggerSecond(nodeId, currTimeInMs, condParams);
			default:
				return false;
			}
		}

		private bool ShouldTriggerSecond(uint nodeId, int currTimeInMs, IMewFeintEvent? condParams)
		{
			if (FeintHelper.IsReady(nodeId))
			{
				return true;
			}
			if (condParams is ReceviceAbilityEffectCondParams receviceAbilityEffectCondParams && receviceAbilityEffectCondParams.ActionId == 47792)
			{
				if (currTimeInMs - 上次岩石弹时间 < 500)
				{
					return false;
				}
				上次岩石弹时间 = currTimeInMs;
				岩石弹Count++;
				if (岩石弹Count >= 2)
				{
					FeintHelper.SetDelay(nodeId, 16000);
				}
			}
			return false;
		}
	}
}
