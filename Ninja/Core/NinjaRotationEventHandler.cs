using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Ninja.PR.Utilities.Data;
using Ninja.PR.Utilities.Settings;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR
{
	public class NinjaRotationEventHandler : IRotationEventHandler
	{
		public async Task OnPreCombat()
		{
		}

		public void OnResetBattle()
		{
			NinjaBattleData.Instance = new NinjaBattleData();
		}

		public async Task OnNoTarget()
		{
			if (!((IBattleChara)(object)Core.Me).HasAura(2839u) && !((IBattleChara)(object)Core.Me).HasAura(3848u) && !Qt.Get("预备水遁") && 36958u.GetSpell().Cooldown.TotalMilliseconds < 3000.0 && AI.Instance.TriggerlineData.CurrTriggerLine == null)
			{
				Qt.Set("预备水遁", qtValue: true);
			}
			await Task.CompletedTask;
		}

		public void OnSpellCastSuccess(Slot slot, Spell spell)
		{
		}

		public void AfterSpell(Slot slot, Spell spell)
		{
			if (spell.Id == 2271 && Qt.Get("预备水遁"))
			{
				Qt.Set("预备水遁", qtValue: false);
				switch (spell.Id)
				{
				case 2259u:
					AI.Instance.BattleData.CurrGcdAbilityCount = 0;
					break;
				case 2261u:
					AI.Instance.BattleData.CurrGcdAbilityCount = 0;
					break;
				case 2263u:
					AI.Instance.BattleData.CurrGcdAbilityCount = 0;
					break;
				case 18805u:
					AI.Instance.BattleData.CurrGcdAbilityCount = 0;
					break;
				case 18806u:
					AI.Instance.BattleData.CurrGcdAbilityCount = 0;
					break;
				case 18807u:
					AI.Instance.BattleData.CurrGcdAbilityCount = 0;
					break;
				case 2271u:
					AI.Instance.BattleData.CurrGcdAbilityCount = 1;
					break;
				case 2267u:
					AI.Instance.BattleData.CurrGcdAbilityCount = 1;
					break;
				case 18881u:
					AI.Instance.BattleData.CurrGcdAbilityCount = 1;
					break;
				case 16492u:
					AI.Instance.BattleData.CurrGcdAbilityCount = 1;
					break;
				}
			}
		}

		public void OnBattleUpdate(int currTimeInMs)
		{
			Drawposer.DrawPosHelper();
		}

		public void OnEnterRotation()
		{
		}

		public void OnExitRotation()
		{
		}

		public void OnTerritoryChanged()
		{
		}
	}
}
