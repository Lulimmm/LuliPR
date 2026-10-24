using AEAssist.Extension;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using AEAssist.CombatRoutine.View.JobView;
using AEAssist.IO;

namespace Ninja.PR.Utilities.Settings
{
	public class NinjaSettings
	{
		public static NinjaSettings Instance;

		private static string path;

		public bool TrickAttackDelayUntilNextMug;

		public bool Kasslockintokunis;

		public bool UseRaiton;

		public bool LockTrickAttackIntoMug = true;

		public bool OpenerBurst;

		public float SuitonIfKCooldownin = 1.5f;

		public bool RaitonWhenK = true;

		public bool RaitonWhenDoku;

		public bool RaitonWhenOutRange;

		public string Rotation = "战技背刺";

		public bool 雷顿背刺;

		public bool Qishou = true;

		public bool LYForYuanli;

		public bool P5PerfectBunshin;

		public string ChooseOpener = "None";

		public bool MoveTcj;


		public float TTK = 1.5f;

		public JobViewSave JobViewSave = new JobViewSave();

		public static void Build(string settingPath)
		{
			path = Path.Combine(settingPath, "NinjaSettings", "Ninja.json");
			if (!File.Exists(path))
			{
				Instance = new NinjaSettings();
				Instance.Save();
				return;
			}
			try
			{
				Instance = JsonHelper.FromJson<NinjaSettings>(File.ReadAllText(path));
			}
			catch (Exception)
			{
				Instance = new NinjaSettings();
			}
		}

		public void Save()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, JsonHelper.ToJson(this));
		}
	}
}
