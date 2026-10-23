using AEAssist.Extension;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Bindings.ImGui;

namespace Ninja.PR.Utilities.Settings
{
	public class NinjaSettingsUI
	{
		public static NinjaSettingsUI Instance = new NinjaSettingsUI();

		public NinjaSettings NinjaSettings => NinjaSettings.Instance;

		public void Draw()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			if (ImGui.Button("Save"))
			{
				NinjaSettings.Instance.Save();
			}
		}
	}
}
