using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Ninja.PR.Utilities.Data;
using Ninja.PR.Utilities.Settings;
using Dalamud.Game.ClientState.Objects.Types;

namespace Ninja.PR.SlotResolver.Ability
{
	public class Mug : ISlotResolver
	{
		public int Check()
		{
			if (!Qt.Get("大背刺"))
			{
				return -110;
			}
			if (GCDHelper.GetGCDCooldown() < 600)
			{
				return -1;
			}
			if (((IGameObject)(object)Core.Me).Distance((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()) > (float)SettingMgr.GetSetting<GeneralSettings>().AttackRange)
			{
				return -2;
			}
			if (36957u.GetSpell().Cooldown.TotalMilliseconds > 0.0 || !36957u.IsUnlock())
			{
				return -3;
			}
			if (((IBattleChara)(object)Core.Me).HasAura(1186u))
			{
				return -4;
			}
			return 0;
		}
		public void Build(Slot slot)
		{
			if (NinjaSettings.Instance.P5PerfectBunshin && Core.Resolve<JobApi_Ninja>().Ninki >= 50)
			{
				slot.Add(ChangeAction(16493u).GetSpell());
			}
			slot.Add(ChangeAction(2248u).GetSpell());
		}

		public static uint ChangeAction(uint SpellId)
		{
			return Core.Resolve<MemApiSpell>().CheckActionChange(SpellId);
		}
	}
}
