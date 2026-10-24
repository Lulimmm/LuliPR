using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine;
using AEAssist.CombatRoutine.View.JobView;
using AEAssist.CombatRoutine.View.JobView.HotkeyResolver;
using AEAssist.Extension;
using AEAssist.Helper;
using Ninja.PR.Opener;
using Ninja.PR.SlotResolver.Ability;
using Ninja.PR.SlotResolver.GCD;
using Ninja.PR.SlotResolver.Opener;
using Ninja.PR.Utilities.MyMethods;
using Ninja.PR.Utilities.Settings;
using Ninja.PR.Utilities.Triggers;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;

namespace Ninja.PR
{
	public class NinjaRotationEntry : IRotationEntry, IDisposable
	{
		private List<SlotResolverData> SlotResolvers = new List<SlotResolverData>
		{
			new SlotResolverData(new _100B(), SlotMode.OffGcd),
			new SlotResolverData(new Kassatsu(), SlotMode.Always),
			new SlotResolverData(new Mug(), SlotMode.OffGcd),
			new SlotResolverData(new TrickAttack(), SlotMode.Always),
			new SlotResolverData(new Bunshin(), SlotMode.OffGcd),
			new SlotResolverData(new TenChiJin(), SlotMode.OffGcd),
			new SlotResolverData(new Meisui(), SlotMode.OffGcd),
			new SlotResolverData(new TenriJindo(), SlotMode.OffGcd),
			new SlotResolverData(new Assassinate(), SlotMode.OffGcd),
			new SlotResolverData(new Bhavacakra(), SlotMode.OffGcd),
			new SlotResolverData(new TrueNorth(), SlotMode.OffGcd),
			new SlotResolverData(new ReadyGoSuiton(), SlotMode.Gcd),
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

		private IOpener M1O = new Raiton2P4();

		private IOpener ED = new EDOpener();

		private IOpener Zhanji = new ZhanjiBeici();

		public string AuthorName { get; set; } = "luli";

		public static JobViewWindow QT { get; private set; }

		public Rotation Build(string settingFolder)
		{
			NinjaSettings.Build(settingFolder);
			BuildQT();
			Rotation rot = new Rotation(SlotResolvers)
			{
				TargetJob = Jobs.Ninja,
				AcrType = AcrType.HighEnd,
				MinLevel = 100,
				MaxLevel = 101,
				Description = "忍者的自动输出ACR 总之 不要死"
			};
			rot.SetRotationEventHandler(new NinjaRotationEventHandler());
			rot.AddTriggerCondition(new TriggerCondition_Ninki());
			rot.AddTriggerAction(new TriggerAction_Qt(), new TriggerAction_Settings(), new TriggerAction_RaitonSettings(), new TriggerAction_RaitonBeici(), new TriggerAction_远离雷遁(), new TriggerAction_P5Pbunshin());
			rot.AddOpener(GetOpener);
			return rot;
		}

		public IRotationUI GetRotationUI()
		{
			return QT;
		}

		public void BuildQT()
		{
			QT = new JobViewWindow(NinjaSettings.Instance.JobViewSave, NinjaSettings.Instance.Save, "Ninja的Qt");
			QT.AddTab("通用", DrawQtGeneral);
			QT.AddTab("Dev", DrawQtDev);
			QT.AddQt("大背刺", qtValueDefault: true, "大背刺");
			QT.AddQt("小背刺", qtValueDefault: true, "小背刺");
			QT.AddQt("天地人", qtValueDefault: true, "天地人");
			QT.AddQt("生杀予夺", qtValueDefault: true, "生杀");
			QT.AddQt("梦幻三段", qtValueDefault: true, "断绝");
			QT.AddQt("水遁", qtValueDefault: true, "水");
			QT.AddQt("雷遁", qtValueDefault: true, "雷");
			QT.AddQt("镰鼬", qtValueDefault: true, "镰鼬");
			QT.AddQt("雷牙", qtValueDefault: true, "雷牙");
			QT.AddQt("分身", qtValueDefault: true, "分身");
			QT.AddQt("命水", qtValueDefault: true, "命水");
			QT.AddQt("六道", qtValueDefault: true, "六道");
			QT.AddQt("天理人道", qtValueDefault: true, "天理人道");
			QT.AddQt("自动真北", qtValueDefault: true, "自动真北");
			QT.AddQt("倾斜资源", qtValueDefault: false, "倾斜资源");
			QT.AddQt("斩杀", qtValueDefault: false, "斩杀");
			QT.AddQt("预备水遁", qtValueDefault: false, "预备水遁");
			QT.AddHotkey("爆发药", new HotKeyResolver_Potion());
			QT.AddHotkey("极限技", new HotKeyResolver_LB());
			QT.AddHotkey("牵制", new HotKeyResolver_NormalSpell(7549u, SpellTargetType.Target, useHighPrioritySlot: true));
			QT.AddHotkey("内丹", new HotKeyResolver_NormalSpell(7541u, SpellTargetType.Self, useHighPrioritySlot: true));
			QT.AddHotkey("亲疏自行", new HotKeyResolver_NormalSpell(7548u, SpellTargetType.Self, useHighPrioritySlot: true));
			QT.AddHotkey("残影", new HotKeyResolver_NormalSpell(2241u, SpellTargetType.Self, useHighPrioritySlot: true));
			QT.AddHotkey("缩地", new NinjaMovement());
			QT.AddHotkey("疾跑", new HotKeyResolver_疾跑());
		}

		public void OnDrawSetting()
		{
			NinjaSettingsUI.Instance.Draw();
		}

		public void OnUIUpdate()
		{
		}

		public void DrawQtGeneral(JobViewWindow jobViewWindow)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0179: Unknown result type (might be due to invalid IL or missing references)
			//IL_0194: Unknown result type (might be due to invalid IL or missing references)
			//IL_019a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01df: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0213: Unknown result type (might be due to invalid IL or missing references)
			//IL_022d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0247: Unknown result type (might be due to invalid IL or missing references)
			//IL_0261: Unknown result type (might be due to invalid IL or missing references)
			//IL_027b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0295: Unknown result type (might be due to invalid IL or missing references)
			//IL_02af: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			if (ImGui.CollapsingHeader(ImU8String.op_Implicit("使用须知"), (ImGuiTreeNodeFlags)0))
			{
				ImGui.Text(ImU8String.op_Implicit("光佬的三插插件设置300ms\nGCD内最大能力技数量设置为2\n关闭全局能力技不卡GCD\n用之前上木桩看一遍是不是自己想要的循环"));
			}
			ImGui.BeginChild(ImU8String.op_Implicit("##tab1"), new Vector2(0f, 0f), false, (ImGuiWindowFlags)0);
			if (ImGui.CollapsingHeader(ImU8String.op_Implicit("ACR设置"), (ImGuiTreeNodeFlags)0))
			{
				string opener = "";
				if (NinjaSettings.Instance.ChooseOpener == "None")
				{
					opener = "无起手";
				}
				else if (NinjaSettings.Instance.ChooseOpener == "Raiton")
				{
					opener = "常规2+4起手";
				}
				else if (NinjaSettings.Instance.ChooseOpener == "Tudun")
				{
					opener = "不常规土遁起手";
				}
				if (ImGui.BeginCombo(ImU8String.op_Implicit("起手选择"), ImU8String.op_Implicit(opener), (ImGuiComboFlags)0))
				{
					if (ImGui.Selectable(ImU8String.op_Implicit("无起手"), false, (ImGuiSelectableFlags)0, default(Vector2)))
					{
						NinjaSettings.Instance.ChooseOpener = "None";
						NinjaSettings.Instance.Save();
					}
					if (ImGui.Selectable(ImU8String.op_Implicit("常规100级起手"), false, (ImGuiSelectableFlags)0, default(Vector2)))
					{
						NinjaSettings.Instance.ChooseOpener = "Raiton";
						NinjaSettings.Instance.Save();
					}
					if (ImGui.Selectable(ImU8String.op_Implicit("不常规100级起手"), false, (ImGuiSelectableFlags)0, default(Vector2)))
					{
						NinjaSettings.Instance.ChooseOpener = "Tudun";
						NinjaSettings.Instance.Save();
					}
					ImGui.EndCombo();
				}
				ImGui.SliderFloat(ImU8String.op_Implicit("TTK当血量剩X%"), ref NinjaSettings.Instance.TTK, 1f, 5f, default(ImU8String), (ImGuiSliderFlags)0);
				ImGui.Text(ImU8String.op_Implicit("杂项"));
				ImGui.Text(ImU8String.op_Implicit("=====ACR选项（除起手爆发药外不要动）====="));
				ImGui.Checkbox(ImU8String.op_Implicit("起手爆发药（防小孩误食）"), ref NinjaSettings.Instance.OpenerBurst);
				ImGui.Checkbox(ImU8String.op_Implicit("生杀锁入小背刺"), ref NinjaSettings.Instance.Kasslockintokunis);
				ImGui.Checkbox(ImU8String.op_Implicit("大小背刺锁死"), ref NinjaSettings.Instance.LockTrickAttackIntoMug);
				ImGui.Checkbox(ImU8String.op_Implicit("下一个小背刺延后到大背刺"), ref NinjaSettings.Instance.TrickAttackDelayUntilNextMug);
				ImGui.Checkbox(ImU8String.op_Implicit("硬砸雷遁"), ref NinjaSettings.Instance.UseRaiton);
				ImGui.Checkbox(ImU8String.op_Implicit("远离打雷遁止损"), ref NinjaSettings.Instance.RaitonWhenOutRange);
				ImGui.Checkbox(ImU8String.op_Implicit("镰鼬仅用作远离"), ref NinjaSettings.Instance.LYForYuanli);
				ImGui.Checkbox(ImU8String.op_Implicit("雷顿背刺"), ref NinjaSettings.Instance.雷顿背刺);
				ImGui.Checkbox(ImU8String.op_Implicit("P5完美分身"), ref NinjaSettings.Instance.P5PerfectBunshin);
			}
			ImGui.EndCombo();
		}

		public void DrawQtDev(JobViewWindow jobViewWindow)
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			string Rotation = "";
			Rotation = ((!NinjaSettings.Instance.雷顿背刺) ? "战技背刺" : "雷顿背刺");
			ImGui.Text(ImU8String.op_Implicit("当前循环: " + Rotation));
			((IEnumerable<IGameObject>)Svc.Objects).FirstOrDefault([NullableContext(0)] (IGameObject obj) => obj.BaseId == 12602);
			ImGui.Text(ImU8String.op_Implicit(((IGameObject)(object)Core.Me).Distance((IGameObject)(object)((IBattleChara)(object)Core.Me).GetCurrTarget()).ToString()));
			ImGui.Text(ImU8String.op_Implicit(GCDHelper.GetElapsedGCD().ToString()));
		}
		private IOpener GetOpener(uint level)
		{
			if (NinjaSettings.Instance.ChooseOpener == "None")
			{
				return null;
			}
			if (NinjaSettings.Instance.雷顿背刺)
			{
				return M1O;
			}
			if (NinjaSettings.Instance.ChooseOpener == "Tudun")
			{
				return ED;
			}
			if (NinjaSettings.Instance.ChooseOpener == "Raiton")
			{
				return new ZhanjiBeici();
			}
			return null;
		}

		public void Dispose()
		{
		}
	}
}
