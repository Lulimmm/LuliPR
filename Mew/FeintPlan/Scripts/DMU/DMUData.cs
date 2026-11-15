using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;
using Dalamud.Game.ClientState.Objects.Types;

namespace Mew.FeintPlan.Scripts.DMU
{
	public static class DMUData
	{
		public static class 读条
		{
			public const uint 玄乎乎魔法 = 47764u;

			public static readonly uint[] 制裁之光 = new uint[2] { 50722u, 47805u };

			public static readonly uint[] 遗弃末世 = new uint[2] { 47804u, 47928u };

			public const uint 过去终结 = 47826u;

			public const uint 未来终结 = 47827u;

			public const uint 终末双腕 = 49740u;

			public const uint 深层痛楚 = 47858u;

			public const uint 地震 = 50546u;

			public const uint 暴雷 = 47881u;

			public const uint 轰击 = 47874u;

			public const uint 大十字 = 47892u;

			public const uint 烈焰 = 47902u;

			public const uint 海啸 = 47903u;

			public const uint 扑腾腾究极 = 49738u;

			public const uint 连续究极 = 47936u;

			public const uint 癫狂交响曲 = 47952u;
		}

		public static class 伤害判定
		{
			public const uint 岩石弹 = 47792u;
		}

		public static string DetectPhaseEntry(IMewFeintEvent? condParams)
		{
			if (condParams is WeatherChangedCondParams weatherChangedCondParams)
			{
				switch (weatherChangedCondParams.NewWeatherId)
				{
				case 79:
					return "P2";
				case 174:
					return "P3";
				case 175:
					return "P5";
				}
			}
			else if (condParams is UnitCreateCondParams unitCreateCondParams)
			{
				IGameObject battleChara = unitCreateCondParams.BattleChara;
				if (battleChara != null && battleChara.BaseId == 19510)
				{
					return "P4";
				}
			}
			return null;
		}
	}
}
