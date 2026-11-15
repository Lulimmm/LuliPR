using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mew.FeintPlan
{
	public static class FeintPresets
	{
		public static List<FeintPreset> GetPresets(uint zoneId)
		{
			switch (zoneId)
			{
			case 1238u:
				return new List<FeintPreset>
				{
					new FeintPreset
					{
						Name = "D1",
						PhaseNodes = new List<FeintPresetNode>
						{
							new FeintPresetNode { Phase = "P1", Name = "暴风破" },
							new FeintPresetNode { Phase = "P1", Name = "光焰圆光#2" },
							new FeintPresetNode { Phase = "P2", Name = "神圣光线#1" },
							new FeintPresetNode { Phase = "P2", Name = "绝对零度#1" },
							new FeintPresetNode { Phase = "P3", Name = "时间压缩·绝" },
							new FeintPresetNode { Phase = "P3", Name = "脉冲星震波" },
							new FeintPresetNode { Phase = "P4", Name = "时间结晶" },
							new FeintPresetNode { Phase = "P5", Name = "星灵之剑#1" },
							new FeintPresetNode { Phase = "P5", Name = "星灵之剑#2" },
							new FeintPresetNode { Phase = "P5", Name = "星灵之剑#3" }
						}
					},
					new FeintPreset
					{
						Name = "D2",
						PhaseNodes = new List<FeintPresetNode>
						{
							new FeintPresetNode { Phase = "P1", Name = "光焰圆光#1" },
							new FeintPresetNode { Phase = "P2", Name = "钻石星尘#1" },
							new FeintPresetNode { Phase = "P2", Name = "光之失控#1" },
							new FeintPresetNode { Phase = "P3", Name = "破盾一击" },
							new FeintPresetNode { Phase = "P4", Name = "光与暗的龙诗#1" },
							new FeintPresetNode { Phase = "P4", Name = "光与暗的龙诗#2" },
							new FeintPresetNode { Phase = "P5", Name = "死亡轮回#1" },
							new FeintPresetNode { Phase = "P5", Name = "死亡轮回#2" },
							new FeintPresetNode { Phase = "P5", Name = "死亡轮回#3" }
						}
					}
				};
			case 1363u:
			{
				int num = 2;
				List<FeintPreset> list13 = new List<FeintPreset>(num);
				CollectionsMarshal.SetCount(list13, num);
				Span<FeintPreset> span13 = CollectionsMarshal.AsSpan(list13);
				ref FeintPreset reference9 = ref span13[0];
				FeintPreset obj9 = new FeintPreset
				{
					Name = "D1"
				};
				int num2 = 9;
				List<FeintPresetNode> list14 = new List<FeintPresetNode>(num2);
				CollectionsMarshal.SetCount(list14, num2);
				Span<FeintPresetNode> span14 = CollectionsMarshal.AsSpan(list14);
				span14[0] = new FeintPresetNode
				{
					Phase = "P1",
					Name = "玄乎乎魔法#1"
				};
				span14[1] = new FeintPresetNode
				{
					Phase = "P1",
					Name = "制裁之光#2"
				};
				span14[2] = new FeintPresetNode
				{
					Phase = "P2",
					Name = "遗弃末世#1"
				};
				span14[3] = new FeintPresetNode
				{
					Phase = "P2",
					Name = "制裁之光#1"
				};
				span14[4] = new FeintPresetNode
				{
					Phase = "P3",
					Name = "深层痛楚#1"
				};
				span14[5] = new FeintPresetNode
				{
					Phase = "P3",
					Name = "地震#1"
				};
				span14[6] = new FeintPresetNode
				{
					Phase = "P4",
					Name = "大十字#1"
				};
				span14[7] = new FeintPresetNode
				{
					Phase = "P5",
					Name = "连续究极#1"
				};
				span14[8] = new FeintPresetNode
				{
					Phase = "P5",
					Name = "遗弃末世#1"
				};
				obj9.PhaseNodes = list14;
				reference9 = obj9;
				ref FeintPreset reference10 = ref span13[1];
				FeintPreset obj10 = new FeintPreset
				{
					Name = "D2"
				};
				num2 = 9;
				List<FeintPresetNode> list15 = new List<FeintPresetNode>(num2);
				CollectionsMarshal.SetCount(list15, num2);
				Span<FeintPresetNode> span15 = CollectionsMarshal.AsSpan(list15);
				span15[0] = new FeintPresetNode
				{
					Phase = "P1",
					Name = "制裁之光#1"
				};
				span15[1] = new FeintPresetNode
				{
					Phase = "P1",
					Name = "玄乎乎魔法#3"
				};
				span15[2] = new FeintPresetNode
				{
					Phase = "P2",
					Name = "过去/未来终结#2"
				};
				span15[3] = new FeintPresetNode
				{
					Phase = "P2",
					Name = "终末双腕#2"
				};
				span15[4] = new FeintPresetNode
				{
					Phase = "P3",
					Name = "暴雷#2"
				};
				span15[5] = new FeintPresetNode
				{
					Phase = "P3",
					Name = "轰击#1"
				};
				span15[6] = new FeintPresetNode
				{
					Phase = "P4",
					Name = "扑腾腾究极#1"
				};
				span15[7] = new FeintPresetNode
				{
					Phase = "P5",
					Name = "连续究极#2"
				};
				span15[8] = new FeintPresetNode
				{
					Phase = "P5",
					Name = "遗弃末世#4"
				};
				obj10.PhaseNodes = list15;
				reference10 = obj10;
				return list13;
			}
			case 1327u:
			{
				int num = 2;
				List<FeintPreset> list10 = new List<FeintPreset>(num);
				CollectionsMarshal.SetCount(list10, num);
				Span<FeintPreset> span10 = CollectionsMarshal.AsSpan(list10);
				ref FeintPreset reference7 = ref span10[0];
				FeintPreset obj7 = new FeintPreset
				{
					Name = "D1"
				};
				int num2 = 11;
				List<string> list11 = new List<string>(num2);
				CollectionsMarshal.SetCount(list11, num2);
				Span<string> span11 = CollectionsMarshal.AsSpan(list11);
				span11[0] = "补天之手#1";
				span11[1] = "补天之手#2";
				span11[2] = "补天之手#3";
				span11[3] = "喋血#1";
				span11[4] = "喋血#3";
				span11[5] = "境中奇焰#1";
				span11[6] = "落火飞溅#1";
				span11[7] = "阴界近/远景#1";
				span11[8] = "林德布鲁姆陨石#1";
				span11[9] = "天顶猛击#1";
				span11[10] = "境中奇狱#2";
				obj7.Nodes = list11;
				reference7 = obj7;
				ref FeintPreset reference8 = ref span10[1];
				FeintPreset obj8 = new FeintPreset
				{
					Name = "D2"
				};
				num2 = 8;
				List<string> list12 = new List<string>(num2);
				CollectionsMarshal.SetCount(list12, num2);
				Span<string> span12 = CollectionsMarshal.AsSpan(list12);
				span12[0] = "致命灾变#1";
				span12[1] = "溅血#1";
				span12[2] = "溅血#2";
				span12[3] = "喋血#2";
				span12[4] = "双重飞踢#1";
				span12[5] = "境中奇梦#1";
				span12[6] = "境中奇奥#1";
				span12[7] = "境中奇狱#1";
				obj8.Nodes = list12;
				reference8 = obj8;
				return list10;
			}
			case 1325u:
			{
				int num = 2;
				List<FeintPreset> list7 = new List<FeintPreset>(num);
				CollectionsMarshal.SetCount(list7, num);
				Span<FeintPreset> span7 = CollectionsMarshal.AsSpan(list7);
				ref FeintPreset reference5 = ref span7[0];
				FeintPreset obj5 = new FeintPreset
				{
					Name = "D1"
				};
				int num2 = 6;
				List<string> list8 = new List<string>(num2);
				CollectionsMarshal.SetCount(list8, num2);
				Span<string> span8 = CollectionsMarshal.AsSpan(list8);
				span8[0] = "天顶的主宰#1";
				span8[1] = "天顶的主宰#2";
				span8[2] = "举世无双的霸王#1";
				span8[3] = "绝命分断击#1";
				span8[4] = "天顶的主宰#3";
				span8[5] = "碎心踢#1";
				obj5.Nodes = list8;
				reference5 = obj5;
				ref FeintPreset reference6 = ref span7[1];
				FeintPreset obj6 = new FeintPreset
				{
					Name = "D2"
				};
				num2 = 5;
				List<string> list9 = new List<string>(num2);
				CollectionsMarshal.SetCount(list9, num2);
				Span<string> span9 = CollectionsMarshal.AsSpan(list9);
				span9[0] = "铸兵突袭#1";
				span9[1] = "统治的战舞#1";
				span9[2] = "大火#2";
				span9[3] = "重陨石#1";
				span9[4] = "双向/四向回旋火#1";
				obj6.Nodes = list9;
				reference6 = obj6;
				return list7;
			}
			case 1323u:
			{
				int num = 2;
				List<FeintPreset> list4 = new List<FeintPreset>(num);
				CollectionsMarshal.SetCount(list4, num);
				Span<FeintPreset> span4 = CollectionsMarshal.AsSpan(list4);
				ref FeintPreset reference3 = ref span4[0];
				FeintPreset obj3 = new FeintPreset
				{
					Name = "D1"
				};
				int num2 = 5;
				List<string> list5 = new List<string>(num2);
				CollectionsMarshal.SetCount(list5, num2);
				Span<string> span5 = CollectionsMarshal.AsSpan(list5);
				span5[0] = "浪顶炽火#1";
				span5[1] = "交错/双重旋水#1";
				span5[2] = "火蛇/水蛇夺浪#1";
				span5[3] = "斗志昂扬#5";
				span5[4] = "斗志昂扬#7";
				obj3.Nodes = list5;
				reference3 = obj3;
				ref FeintPreset reference4 = ref span4[1];
				FeintPreset obj4 = new FeintPreset
				{
					Name = "D2"
				};
				num2 = 5;
				List<string> list6 = new List<string>(num2);
				CollectionsMarshal.SetCount(list6, num2);
				Span<string> span6 = CollectionsMarshal.AsSpan(list6);
				span6[0] = "火浪回切#1";
				span6[1] = "斗志昂扬#3";
				span6[2] = "斗志昂扬#4";
				span6[3] = "极限火蛇/水蛇夺浪#1";
				span6[4] = "斗志昂扬#8";
				obj4.Nodes = list6;
				reference4 = obj4;
				return list4;
			}
			case 1321u:
			{
				int num = 2;
				List<FeintPreset> list = new List<FeintPreset>(num);
				CollectionsMarshal.SetCount(list, num);
				Span<FeintPreset> span = CollectionsMarshal.AsSpan(list);
				ref FeintPreset reference = ref span[0];
				FeintPreset obj = new FeintPreset
				{
					Name = "D1"
				};
				int num2 = 5;
				List<string> list2 = new List<string>(num2);
				CollectionsMarshal.SetCount(list2, num2);
				Span<string> span2 = CollectionsMarshal.AsSpan(list2);
				span2[0] = "魅亡之音#1";
				span2[1] = "施虐的尖啸#2";
				span2[2] = "贪欲无厌#1";
				span2[3] = "血锁牢狱#2";
				span2[4] = "贪欲无厌#2";
				obj.Nodes = list2;
				reference = obj;
				ref FeintPreset reference2 = ref span[1];
				FeintPreset obj2 = new FeintPreset
				{
					Name = "D2"
				};
				num2 = 5;
				List<string> list3 = new List<string>(num2);
				CollectionsMarshal.SetCount(list3, num2);
				Span<string> span3 = CollectionsMarshal.AsSpan(list3);
				span3[0] = "粗暴之雨#1";
				span3[1] = "致命的闭幕曲#1";
				span3[2] = "粗暴之雨#2";
				span3[3] = "施虐的尖啸#4";
				span3[4] = "粗暴之雨#3";
				obj2.Nodes = list3;
				reference2 = obj2;
				return list;
			}
			default:
				return new List<FeintPreset>();
			}
		}
	}
}
