using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Ninja.PR.SlotResolver.GCD;
using Ninja.PR.Utilities.Data;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.Ability
{
	public class TrueNorth : ISlotResolver
	{
		private List<SlotResolverData> SlotResolvers = new List<SlotResolverData>
		{
			new SlotResolverData(new TCJAction(), SlotMode.Gcd),
			new SlotResolverData(new KassatsuAction(), SlotMode.Gcd),
			new SlotResolverData(new Suiton(), SlotMode.Gcd),
			new SlotResolverData(new earlier4or3(), SlotMode.Gcd),
			new SlotResolverData(new KBase(), SlotMode.Gcd),
			new SlotResolverData(new PhantomKamaitachi(), SlotMode.Gcd),
			new SlotResolverData(new FleetingRaiju(), SlotMode.Gcd),
			new SlotResolverData(new Raiton(), SlotMode.Gcd),
			new SlotResolverData(new BaseCombo(), SlotMode.Gcd)
		};

		public SlotResolverData FindFirstCheckZero()
		{
			foreach (SlotResolverData resolver in SlotResolvers)
			{
				if (resolver.SlotResolver.Check() >= 0)
				{
					return resolver;
				}
			}
			return null;
		}

		public int Check()
		{
			if (!Qt.Get("自动真北"))
			{
				return -110;
			}
			uint Last = Core.Resolve<MemApiSpell>().GetLastComboSpellId();
			bool Flanking = Core.Resolve<MemApiTarget>().IsFlanking;
			bool Behind = Core.Resolve<MemApiTarget>().IsBehind;
			if (7546u.RecentlyUsed(2120) || ((IBattleChara)(object)Core.Me).HasAura(1250u))
			{
				return -1;
			}
			if (7546u.GetSpell().Charges < 1f)
			{
				return -2;
			}
			if (GCDHelper.GetGCDCooldown() < 600)
			{
				return -3;
			}
			if (Core.Resolve<MemApiSpell>().GetElapsedGCD() <= Core.Resolve<MemApiSpell>().GetGCDDuration() / 8 * 5)
			{
				return -4;
			}
			if (!((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()).HasPositional())
			{
				return -5;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -4;
			}
			if (36958u.GetSpell().Cooldown.TotalMilliseconds < 2120.0 && Qt.Get("小背刺"))
			{
				return -5;
			}
			if (FindFirstCheckZero()?.SlotResolver is BaseCombo && Last == 2242)
			{
				if (Core.Resolve<JobApi_Ninja>().Kazematoi == 0 && !Flanking)
				{
					return 1;
				}
				if (Core.Resolve<JobApi_Ninja>().Kazematoi > 3 && !Behind)
				{
					return 2;
				}
				if (!Flanking && !Behind)
				{
					return 3;
				}
				if (((2248u.GetSpell().Cooldown.TotalMilliseconds < 8000.0 && Core.Resolve<JobApi_Ninja>().Kazematoi <= 1) || (36958u.GetSpell().Cooldown.TotalMilliseconds < 8000.0 && Core.Resolve<JobApi_Ninja>().Kazematoi <= 1)) && !Flanking)
				{
					return 4;
				}
			}
			return -1;
		}

		public void Build(Slot slot)
		{
			slot.Add(7546u.GetSpell());
		}
	}
}
