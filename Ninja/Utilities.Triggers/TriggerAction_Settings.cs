using AEAssist.Extension;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using AEAssist.CombatRoutine.Trigger;
using Ninja.PR.Utilities.Settings;
using Dalamud.Bindings.ImGui;

namespace Ninja.PR.Utilities.Triggers
{
	public class TriggerAction_Settings : ITriggerAction, ITriggerBase
	{
		public bool TADUN;

		public string ValueName { get; set; } = new string("".AsSpan());

		public bool Value { get; set; }

		public string DisplayName => "Ninja/下一个小背刺是否延后至大背刺";

		public string Remark { get; set; }

		public bool Draw()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			if (ImGui.BeginTabBar(ImU8String.op_Implicit("###PLM"), (ImGuiTabBarFlags)0))
			{
				if (ImGui.BeginTabItem(ImU8String.op_Implicit("Ninja"), (ImGuiTabItemFlags)0))
				{
					ImGui.BeginChild(ImU8String.op_Implicit("###PLNM"), new Vector2(0f, 0f), false, (ImGuiWindowFlags)0);
					ImGui.Checkbox(ImU8String.op_Implicit("下一个小背刺延后到大背刺里"), ref TADUN);
					ImGui.EndChild();
					ImGui.EndTabItem();
				}
				ImGui.EndTabBar();
			}
			return true;
		}

		public bool Handle()
		{
			NinjaSettings.Instance.TrickAttackDelayUntilNextMug = TADUN;
			return true;
		}

		public void Check()
		{
		}
	}
}
