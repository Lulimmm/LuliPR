using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.Extension;
using Ninja.PR.SlotResolver.GCD;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.Methods
{
	public class SlotResolverManager
	{
		private static List<ISlotResolver> SlotResolvers = new List<ISlotResolver>
		{
			new TCJAction(),
			new KassatsuAction(),
			new Suiton(),
			new earlier4or3(),
			new KBase(),
			new PhantomKamaitachi(),
			new FleetingRaiju(),
			new Raiton(),
			new BaseCombo()
		};

		public static ISlotResolver GetFirstValidSlotResolver()
		{
			if (((IBattleChara)(object)Core.Me).GetCurrTarget() != null)
			{
				foreach (ISlotResolver resolver in SlotResolvers)
				{
					if (resolver.Check() >= 0)
					{
						return resolver;
					}
				}
				return null;
			}
			return null;
		}
	}
}
