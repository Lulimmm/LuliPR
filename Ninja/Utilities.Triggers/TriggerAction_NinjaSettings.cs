using AEAssist.Extension;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using AEAssist.CombatRoutine.Trigger;
using Dalamud.Bindings.ImGui;

namespace Ninja.PR.Utilities.Triggers
{
	public class TriggerAction_NinjaSettings : ITriggerAction, ITriggerBase
	{
		private int 当前combo;

		private int radioCheck;

		private int radioType;

		public string ValueName { get; set; } = new string("".AsSpan());

		public bool Value { get; set; }

		public string DisplayName => "Ninja/ACR设置";

		public string Remark { get; set; }

		public bool Draw()
		{
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			string[] qtArray = NinjaRotationEntry.QT.GetQtArray();
			当前combo = Array.IndexOf(qtArray, ValueName);
			if (当前combo == -1)
			{
				当前combo = 0;
			}
			radioCheck = ((!Value) ? 1 : 0);
			if (ImGui.BeginTabBar(ImU8String.op_Implicit("###TriggerTab"), (ImGuiTabBarFlags)0))
			{
				if (ImGui.BeginTabItem(ImU8String.op_Implicit("Ninja"), (ImGuiTabItemFlags)0))
				{
					ImGui.BeginChild(ImU8String.op_Implicit("###TriggerNinja"), new Vector2(0f, 0f), false, (ImGuiWindowFlags)0);
					if (radioType == 0)
					{
						ImGui.Combo(ImU8String.op_Implicit("Qt开关"), ref 当前combo, (ReadOnlySpan<string>)qtArray, qtArray.Length);
						ValueName = qtArray[当前combo];
						ImGui.RadioButton<int>(ImU8String.op_Implicit("开"), ref radioCheck, 0);
						ImGui.SameLine();
						ImGui.RadioButton<int>(ImU8String.op_Implicit("关"), ref radioCheck, 1);
						Value = radioCheck == 0;
					}
					ImGui.EndChild();
					ImGui.EndTabItem();
				}
				ImGui.EndTabBar();
			}
			return true;
		}

		public bool Handle()
		{
			NinjaRotationEntry.QT.SetQt(ValueName, Value);
			return true;
		}

		public void Check()
		{
		}
	}
}
