using AEAssist.Extension;
using System;
using System.Numerics;
using AEAssist;
using AEAssist.CombatRoutine.View.JobView;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Ninja.PR.Utilities.MyMethods
{
	public class NinjaMovement : IHotkeyResolver
	{
		private unsafe static RaptureAtkModule* RaptureAtkModule => ((UIModule)((Framework)Framework.Instance()).GetUIModule()).GetRaptureAtkModule();

		public void DrawExternal(Vector2 size, bool isActive)
		{
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			Vector2 size2 = size * 0.8f;
			ImGui.SetCursorPos(size * 0.1f);
			if (!2262u.IsUnlockWithCDCheck() && Core.Resolve<MemApiIcon>().GetActionTexture(2262u, out var textureWrap2))
			{
				ImGui.Image(textureWrap2.Handle, size2);
			}
		}

		public void Draw(Vector2 size)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Vector2 size2 = size * 0.8f;
			ImGui.SetCursorPos(size * 0.1f);
			if (Core.Resolve<MemApiIcon>().GetActionTexture(2262u, out var textureWrap))
			{
				ImGui.Image(textureWrap.Handle, size2);
			}
		}

		public int Check()
		{
			if (!2262u.IsUnlockWithCDCheck())
			{
				return -1;
			}
			return 0;
		}

		public static Vector3 向量位移(Vector3 position, float facingRadians, float distance)
		{
			float dx = (float)(Math.Sin(facingRadians) * (double)distance);
			float dz = (float)(Math.Cos(facingRadians) * (double)distance);
			return new Vector3(position.X + dx, position.Y + 5f, position.Z + dz);
		}

		internal unsafe static float GetCameraRotation()
		{
			int num = ((NumberArrayData)((AtkArrayDataHolder)(&((AtkModule)(&((RaptureAtkModule)RaptureAtkModule).AtkModule)).AtkArrayDataHolder)).NumberArrays[24]).IntArray[3];
			int sign = ((Math.Sign(num) != -1) ? 1 : (-1));
			return (float)((Math.Abs((double)num * (Math.PI / 180.0)) - Math.PI) * (double)sign);
		}

		public void Run()
		{
			Core.Resolve<MemApiSpell>().Cast(2262u, 向量位移(((IGameObject)Core.Me).Position, GetCameraRotation(), 20f));
		}
	}
}
