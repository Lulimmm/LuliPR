using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.GCD
{
	public class BaseCombo : ISlotResolver
	{
		public int Check()
		{
			if (((IGameObject)(object)Core.Me).Distance((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()) > (float)SettingMgr.GetSetting<GeneralSettings>().AttackRange)
			{
				return -1;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(2690u))
			{
				return -2;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -3;
			}
			return 0;
		}

		private Spell GetSpell()
		{
			uint Last = Core.Resolve<MemApiSpell>().GetLastComboSpellId();
			bool Flanking = Core.Resolve<MemApiTarget>().IsFlanking;
			_ = Core.Resolve<MemApiTarget>().IsBehind;
			TargetHelper.GetNearbyEnemyCount(3);
			if (!((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()).HasPositional() && Last == 2242)
			{
				if (Core.Resolve<JobApi_Ninja>().Kazematoi == 0)
				{
					return 3563u.GetSpell();
				}
				return 2255u.GetSpell();
			}
			if (Last == 2242 && ((ICharacter)Core.Me).Level >= 26)
			{
				if (((ICharacter)Core.Me).Level < 54)
				{
					return 2255u.GetSpell();
				}
				if ((NinjaRotationEntry.QT.GetQt("小背刺") || NinjaRotationEntry.QT.GetQt("大背刺")) && ((2248u.GetSpell().Cooldown.TotalMilliseconds < 8000.0 && Core.Resolve<JobApi_Ninja>().Kazematoi <= 1) || (36958u.GetSpell().Cooldown.TotalMilliseconds < 8000.0 && Core.Resolve<JobApi_Ninja>().Kazematoi <= 1)))
				{
					return 3563u.GetSpell();
				}
				if (((IBattleChara)(object)Core.Me).GetCurrTarget().HasAura(3906u) && Core.Resolve<JobApi_Ninja>().Kazematoi != 0)
				{
					return 2255u.GetSpell();
				}
				if (Core.Resolve<JobApi_Ninja>().Kazematoi == 0)
				{
					return 3563u.GetSpell();
				}
				if (Core.Resolve<JobApi_Ninja>().Kazematoi <= 3 && Flanking)
				{
					return 3563u.GetSpell();
				}
				if (Core.Resolve<JobApi_Ninja>().Kazematoi != 0)
				{
					return 2255u.GetSpell();
				}
			}
			if (Last == 2240 && ((ICharacter)Core.Me).Level >= 4)
			{
				return 2242u.GetSpell();
			}
			return 2240u.GetSpell();
		}

		public void Build(Slot slot)
		{
			Spell spell = GetSpell();
			if (spell != null)
			{
				slot.Add(spell);
			}
		}

		public Spell Spellid()
		{
			return GetSpell();
		}
	}
}
