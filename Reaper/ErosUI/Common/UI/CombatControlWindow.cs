using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using PromeRotation.Data;

namespace ErosUI;

// 网站风格的像素小人控制器。尺寸、资源和交互对齐网站 desk-pet：
// 左键点击切换运行/关闭，右键打开设置，中键锁定自动移动，左键拖动移动位置。
public sealed partial class CombatControlWindow : Window
{
    private enum PetVisualState
    {
        Idle,
        Drag,
        Jump,
        Fly,
        Cool,
    }

    private static readonly Vector2 DefaultPosition = new(32f, 320f);
    private static readonly Vector2 PetSize = new(120f, 120f);
    private static readonly Random PetRandom = new();
    private static readonly string[][] IdlePetAnimations =
    {
        Frames("pet.nothing_nothing", 16),
        Frames("pet.hu_hu", 25),
    };
    private static readonly string[][] DragPetAnimations =
    {
        Frames("pet.happy_happy", 7),
        Frames("pet.happy__2__happy__2_", 8),
    };
    private static readonly string[][] JumpPetAnimations =
    {
        Frames("pet.jump_jump", 7),
        Frames("pet.jump__2__jump__2_", 8),
    };
    private static readonly string[] FlyPetFrames =
    {
        "pet.fly_01.png", "pet.fly_02.png", "pet.fly_03.png", "pet.fly_04.png",
        "pet.fly_05.png", "pet.fly_06.png", "pet.fly_07.png", "pet.fly_08.png",
    };
    private static readonly string[] CoolPetFrames = Frames("pet.cool_cool", 37);
    private static readonly string[] SealPetFrames =
    {
        "pet.seal_01.png", "pet.seal_02.png", "pet.seal_03.png",
        "pet.seal_04.png", "pet.seal_05.png", "pet.seal_06.png",
    };
    private static readonly string[] OffTooltips =
    {
        "左键：运行\n右键：开关设置\n中键：锁定自动移动\n拖动：移动像素小人",
        "左键：运行\n右键：开关设置\n中键：解锁自动移动\n拖动：移动像素小人",
    };
    private static readonly string[] ActiveTooltips =
    {
        "左键：关闭\n右键：开关设置\n中键：锁定自动移动\n拖动：移动像素小人",
        "左键：关闭\n右键：开关设置\n中键：解锁自动移动\n拖动：移动像素小人",
    };

    private readonly System.Action toggleSettings;
    private readonly Func<Vector2>? getSavedPosition;
    private readonly System.Action<Vector2>? savePosition;
    private readonly System.Action<bool>? savePositionLocked;
    private bool persistPending;
    private bool petDragging;
    private bool positionLocked;
    private bool petStateInitialized;
    private PetVisualState petState = PetVisualState.Idle;
    private int petVariant;
    private double petStateStartedAt;
    private double nextIdleVariantAt;
    private double nextFlyAt;
    private double lastPetInteractionAt;
    private bool pendingPetClick;
    private double pendingPetClickAt;
    private bool suppressNextPetClickRelease;
    // Keep names only, never texture-wrap instances. A wrap is owned by Dalamud
    // and can be replaced/disposed between frames.
    private string? lastReadyPetFrame;
    private string? lastReadySealFrame;
    private Vector2 petFlyFrom;
    private Vector2 petFlyTarget;
    private double petFlyEndsAt;
    private bool petFacingLeft;

    private static string[] Frames(string prefix, int count)
    {
        var frames = new string[count];
        for (var i = 0; i < count; i++)
            frames[i] = $"{prefix}_{i + 1:00}.png";
        return frames;
    }

    public CombatControlWindow(
        string title,
        System.Action toggleSettings,
        Func<Vector2>? getSavedPosition = null,
        System.Action<Vector2>? savePosition = null,
        Func<bool>? getPositionLocked = null,
        System.Action<bool>? savePositionLocked = null)
        : base(title,
            ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav)
    {
        this.toggleSettings = toggleSettings;
        this.getSavedPosition = getSavedPosition;
        this.savePosition = savePosition;
        this.savePositionLocked = savePositionLocked;
        positionLocked = getPositionLocked?.Invoke() ?? false;

        Position = getSavedPosition?.Invoke() ?? DefaultPosition;
        PositionCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = false;
        AllowBackgroundBlur = false;
        IsOpen = true;
    }

    public override void PreDraw()
    {
        // 本窗口独立使用 SimplePalette 颜色，需自行同步色彩方案（设置窗口可能未打开）
        SimplePalette.ApplyScheme(ErosUICommonSettings.Instance.UIMode == SettingsUIMode.Day
            ? SimplePalette.UIColorScheme.Light
            : SimplePalette.UIColorScheme.Dark);

        // 透明背景：底色直接画在 background drawList，不经 WindowBg
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.Border, SimplePalette.Border);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        // UI 线程节流压制 PR 本体面板（1 秒一次，防止自动弹回）
        ErosUIFramework.HidePrPanels();
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        base.PreDraw();
    }

    public override void Draw()
    {
        DrawPet();
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(4);
        ImGui.PopStyleColor(2);
        base.PostDraw();
    }

    private void DrawPet()
    {
        UpdatePetState();
        var now = ImGui.GetTime();
        var doubleClickTime = Math.Max(0.30d, ImGui.GetIO().MouseDoubleClickTime);
        var pos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        ImGui.InvisibleButton("##cc_pet", PetSize);
        var hovered = ImGui.IsItemHovered();
        var rightClicked = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var middleClicked = ImGui.IsItemClicked(ImGuiMouseButton.Middle);
        var doubleClicked = hovered && pendingPetClick
            && now - pendingPetClickAt <= doubleClickTime
            && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);

        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left)
            && pendingPetClick && !doubleClicked
            && now - pendingPetClickAt > doubleClickTime)
        {
            pendingPetClick = false;
            ToggleAcrState();
        }

        if (doubleClicked)
        {
            // A double click has its own action. Cancel the delayed single-click
            // action so it cannot also toggle the ACR state.
            pendingPetClick = false;
            suppressNextPetClickRelease = true;
            ErosUIFramework.ToggleAuxiliaryPanels();
        }

        if (hovered && (ImGui.IsMouseClicked(ImGuiMouseButton.Left)
                        || ImGui.IsMouseClicked(ImGuiMouseButton.Right)
                        || middleClicked))
        {
            lastPetInteractionAt = ImGui.GetTime();
            if (petState == PetVisualState.Cool)
            {
                petState = PetVisualState.Idle;
                petStateStartedAt = lastPetInteractionAt;
                nextIdleVariantAt = lastPetInteractionAt + 8d;
            }
        }

        if (hovered)
            ImGui.SetMouseCursor(petDragging ? ImGuiMouseCursor.ResizeAll : ImGuiMouseCursor.Hand);

        if (ImGui.IsItemActive() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            if (!petDragging && ImGui.GetIO().MouseDelta.LengthSquared() > 4f)
            {
                petDragging = true;
                petState = PetVisualState.Drag;
                petVariant = 0;
                petStateStartedAt = ImGui.GetTime();
            }
            if (petDragging)
            {
                ImGui.SetWindowPos(限制到屏幕内(ImGui.GetWindowPos() + ImGui.GetIO().MouseDelta));
                persistPending = true;
            }
        }

        if (ImGui.IsItemDeactivated())
        {
            if (!petDragging && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                if (suppressNextPetClickRelease)
                    suppressNextPetClickRelease = false;
                else if (!doubleClicked)
                {
                    pendingPetClick = true;
                    pendingPetClickAt = now;
                }
            }
            else if (petDragging)
            {
                petState = PetVisualState.Idle;
                petVariant = Math.Clamp(petVariant, 0, IdlePetAnimations.Length - 1);
                petStateStartedAt = ImGui.GetTime();
                nextIdleVariantAt = petStateStartedAt + 8d;
            }
            petDragging = false;
        }

        if (suppressNextPetClickRelease && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            suppressNextPetClickRelease = false;

        if (rightClicked)
            toggleSettings();

        if (middleClicked)
        {
            positionLocked = !positionLocked;
            if (positionLocked)
            {
                if (petState == PetVisualState.Fly)
                {
                    petState = PetVisualState.Idle;
                    petStateStartedAt = ImGui.GetTime();
                    nextIdleVariantAt = petStateStartedAt + 8d;
                }
                savePosition?.Invoke(ImGui.GetWindowPos());
            }
            savePositionLocked?.Invoke(positionLocked);
        }

        if (persistPending && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            persistPending = false;
            savePosition?.Invoke(ImGui.GetWindowPos());
        }

        if (pendingPetClick && !ImGui.IsMouseDown(ImGuiMouseButton.Left)
            && now - pendingPetClickAt >= doubleClickTime + 0.05d)
        {
            pendingPetClick = false;
            ToggleAcrState();
        }
        var acrOff = PromeSettings.Instance.EnableAcr == AcrState.Off;
        var frame = acrOff ? GetSealFrame(now) : GetPetFrame(now);
        var drawFrame = frame;
        var texture = ErosUILayer.GetResourceTextureWrap(frame);
        if (!IsReadyTexture(texture))
        {
            var fallbackFrame = acrOff ? lastReadySealFrame : lastReadyPetFrame;
            if (fallbackFrame != null)
            {
                drawFrame = fallbackFrame;
                texture = ErosUILayer.GetResourceTextureWrap(fallbackFrame);
            }
        }

        if (IsReadyTexture(texture))
        {
            if (acrOff) lastReadySealFrame = drawFrame;
            else lastReadyPetFrame = drawFrame;

            try
            {
                // 网站通过 scaleX(-1) 翻转朝向；对 ImGui 纹理交换 U 坐标即可得到同样效果。
                var uv0 = petFacingLeft ? new Vector2(1f, 0f) : Vector2.Zero;
                var uv1 = petFacingLeft ? new Vector2(0f, 1f) : Vector2.One;
                drawList.AddImage(texture!.Handle, pos, pos + PetSize, uv0, uv1,
                    SimplePalette.ToU32(new Vector4(1f, 1f, 1f, hovered ? 1f : 0.96f)));
            }
            catch (ObjectDisposedException)
            {
                // Keep the frame name. The shared texture may be recreated on
                // the next UI frame after a device/resource reset; clearing it
                // here would turn a short reload into visible flicker.
            }
        }

        if (hovered)
            DrawPetTooltip((PromeSettings.Instance.EnableAcr == AcrState.Off
                ? OffTooltips
                : ActiveTooltips)[positionLocked ? 1 : 0]);

    }

    private static void DrawPetTooltip(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.PopupBg, SimplePalette.PopupBg);
        ImGui.PushStyleColor(ImGuiCol.Text, SimplePalette.TextPrimary);
        ImGui.PushStyleColor(ImGuiCol.Border, SimplePalette.BorderStrong);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 6f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 8f));
        ImGui.BeginTooltip();
        ImGui.TextUnformatted(text);
        ImGui.EndTooltip();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
    }

    private void ToggleAcrState()
    {
        var state = PromeSettings.Instance.EnableAcr;
        PromeSettings.Instance.EnableAcr = state == AcrState.Off ? AcrState.On : AcrState.Off;
        petState = PetVisualState.Jump;
        petVariant = (petVariant + 1) % JumpPetAnimations.Length;
        petStateStartedAt = ImGui.GetTime();
    }

    private static bool IsReadyTexture(IDalamudTextureWrap? texture)
    {
        if (texture == null) return false;
        try
        {
            // GetWrapOrEmpty returns a valid empty wrap while an embedded frame
            // is still loading. Do not replace a visible frame with that wrap.
            return texture.Width > 1 && texture.Height > 1;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private void UpdatePetState()
    {
        var now = ImGui.GetTime();
        if (!petStateInitialized)
        {
            petStateInitialized = true;
            petVariant = PetRandom.Next(IdlePetAnimations.Length);
            petStateStartedAt = now;
            nextIdleVariantAt = now + 8d;
            nextFlyAt = now + 20d;
            lastPetInteractionAt = now;
        }

        if (positionLocked && petState == PetVisualState.Fly)
        {
            petState = PetVisualState.Idle;
            petStateStartedAt = now;
            nextIdleVariantAt = now + 8d;
        }

        if (petState == PetVisualState.Fly)
        {
            var progress = petFlyEndsAt <= petStateStartedAt
                ? 1f
                : Math.Clamp((float)((now - petStateStartedAt) / (petFlyEndsAt - petStateStartedAt)), 0f, 1f);
            // Website uses a 4 second ease-in-out transition. SmoothStep keeps the
            // same soft start/end while allowing ImGui to update the window position.
            var eased = progress * progress * (3f - 2f * progress);
            ImGui.SetWindowPos(限制到屏幕内(Vector2.Lerp(petFlyFrom, petFlyTarget, eased)));
            if (progress >= 1f)
            {
                petState = PetVisualState.Idle;
                petStateStartedAt = now;
                nextIdleVariantAt = now + 8d;
            }
            return;
        }

        if (petState == PetVisualState.Jump && now - petStateStartedAt >= 1.2d)
        {
            petState = PetVisualState.Idle;
            petStateStartedAt = now;
            nextIdleVariantAt = now + 8d;
        }

        if (petState == PetVisualState.Drag)
            petVariant = (int)((now - petStateStartedAt) / 0.4d) % DragPetAnimations.Length;

        if (petState == PetVisualState.Idle)
        {
            if (now >= nextIdleVariantAt)
            {
                petVariant = NextVariant(IdlePetAnimations.Length, petVariant);
                nextIdleVariantAt = now + 8d;
            }

            if (!positionLocked && !petDragging && !ImGui.IsMouseDown(ImGuiMouseButton.Left) && now >= nextFlyAt)
                StartPetFlight(now);

            // The website enters its cool animation after three minutes without
            // pointer/keyboard activity. A pet click wakes it immediately above.
            if (!petDragging && now - lastPetInteractionAt >= 180d)
            {
                petState = PetVisualState.Cool;
                petVariant = 0;
                petStateStartedAt = now;
            }
        }
    }

    private void StartPetFlight(double now)
    {
        var viewport = ImGui.GetMainViewport();
        var min = viewport.WorkPos + new Vector2(4f);
        var max = viewport.WorkPos + viewport.WorkSize - PetSize - new Vector2(4f);
        if (max.X < min.X) max.X = min.X;
        if (max.Y < min.Y) max.Y = min.Y;

        petFlyFrom = ImGui.GetWindowPos();
        petFlyTarget = new Vector2(
            min.X + (float)PetRandom.NextDouble() * MathF.Max(0f, max.X - min.X),
            min.Y + (float)PetRandom.NextDouble() * MathF.Max(0f, max.Y - min.Y));
        petFacingLeft = petFlyTarget.X < petFlyFrom.X;
        petState = PetVisualState.Fly;
        petStateStartedAt = now;
        petFlyEndsAt = now + 4d;
        nextFlyAt = now + 20d;
    }

    private string GetPetFrame(double now)
    {
        string[] frames;
        var animationTime = Math.Max(0d, now - petStateStartedAt);
        switch (petState)
        {
            case PetVisualState.Drag:
                frames = DragPetAnimations[petVariant % DragPetAnimations.Length];
                break;
            case PetVisualState.Jump:
                frames = JumpPetAnimations[petVariant % JumpPetAnimations.Length];
                break;
            case PetVisualState.Fly:
                frames = FlyPetFrames;
                break;
            case PetVisualState.Cool:
                frames = CoolPetFrames;
                break;
            default:
                frames = IdlePetAnimations[petVariant % IdlePetAnimations.Length];
                break;
        }

        var frameIndex = (int)(animationTime * 9.09d) % frames.Length;
        return frames[frameIndex];
    }

    private string GetSealFrame(double now)
    {
        var frameIndex = (int)(Math.Max(0d, now - petStateStartedAt) * 6.67d) % SealPetFrames.Length;
        return SealPetFrames[frameIndex];
    }

    private static int NextVariant(int count, int current)
    {
        if (count <= 1) return 0;
        var next = PetRandom.Next(count - 1);
        return next >= current ? next + 1 : next;
    }

    private void DrawBackground()
    {
        if (ImGui.GetWindowViewport().ID != ImGui.GetMainViewport().ID) return;

        // 底色画到视口背景层即可（本窗口是 NoDecoration 小窗，不参与背景模糊，
        // 无需像其他窗口那样垫占位命令）。矩形整体内缩半像素对齐像素中心，
        // 保证圆角与半透明描边的抗锯齿正常。
        var min = ImGui.GetWindowPos() + new Vector2(0.5f);
        var max = min + ImGui.GetWindowSize() - new Vector2(1f);
        // 圆角必须与 PreDraw 压入的 WindowRounding(10f) 完全一致，否则模糊/底色的角与窗口边框错位
        var rounding = 10f;
        var drawList = ImGui.GetBackgroundDrawList(ImGui.GetWindowViewport());

        var day = ErosUICommonSettings.Instance.UIMode == SettingsUIMode.Day;
        ErosUILayer.DrawWebsiteSurface(drawList, min, max, ModernBarTint(ErosUICommonSettings.Instance.UIMode),
            SimplePalette.BorderStrong, rounding, day ? 0.30f : 0.38f);
    }

    // 顶部 6px 拖动区：短击空白不切换状态，仅负责拖动；松手后写盘一次。
    // 宽度用固定 RowWidth：取 ContentRegionAvail 会随窗口宽度自馈，AlwaysAutoResize 收不回去
    private void DrawDragZone()
    {
        ImGui.InvisibleButton("##cc_drag", new Vector2(RowWidth, 6f));
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            ImGui.SetWindowPos(限制到屏幕内(ImGui.GetWindowPos() + ImGui.GetIO().MouseDelta));
            persistPending = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);

        // 拖动结束（松手）后落盘一次，避免拖动过程频繁写 JSON
        if (persistPending && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            persistPending = false;
            savePosition?.Invoke(ImGui.GetWindowPos());
        }
    }

    private void HandlePanelVisibilityDoubleClick()
    {
        if (!ImGui.IsWindowHovered() || !ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            return;

        var mouse = ImGui.GetIO().MousePos;
        var content = ImGui.GetWindowPos() + ImGui.GetStyle().WindowPadding;
        var rowTop = content.Y + 6f;

        // 排除三个按钮的完整矩形，避免双击按钮同时触发面板显隐。
        var state = new Vector4(content.X, rowTop, StateButtonWidth, ModernButtonHeight);
        var autoPull = new Vector4(content.X + StateButtonWidth + DividerAdvance, rowTop,
            AutoPullButtonWidth, ModernButtonHeight);
        var settings = new Vector4(content.X + StateButtonWidth + DividerAdvance + AutoPullButtonWidth + DividerAdvance,
            rowTop, SettingsButtonWidth, ModernButtonHeight);

        if (在矩形内(mouse, state) || 在矩形内(mouse, autoPull) || 在矩形内(mouse, settings))
            return;

        ErosUIFramework.ToggleAuxiliaryPanels();
    }

    private static bool 在矩形内(Vector2 point, Vector4 rect) =>
        point.X >= rect.X && point.X <= rect.X + rect.Z
        && point.Y >= rect.Y && point.Y <= rect.Y + rect.W;

    // 把窗口位置限制在屏幕工作区内（含 4px 边距）；窗口尺寸未知时按全屏处理。
    private Vector2 限制到屏幕内(Vector2 pos)
    {
        var vp = ImGui.GetMainViewport();
        var size = ImGui.GetWindowSize();
        if (size.X <= 0f || size.Y <= 0f) size = PetSize;   // 首帧兜底尺寸

        var margin = 4f;
        var minX = vp.WorkPos.X + margin;
        var minY = vp.WorkPos.Y + margin;
        var maxX = vp.WorkPos.X + vp.WorkSize.X - size.X - margin;
        var maxY = vp.WorkPos.Y + vp.WorkSize.Y - size.Y - margin;
        if (maxX < minX) maxX = minX;
        if (maxY < minY) maxY = minY;

        return new Vector2(Math.Clamp(pos.X, minX, maxX), Math.Clamp(pos.Y, minY, maxY));
    }

    private void DrawSettingsButton()
    {
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var size = new Vector2(SettingsButtonWidth, ModernButtonHeight);
        var max = pos + size;

        ImGui.InvisibleButton("##cc_settings", size);
        var hovered = ImGui.IsItemHovered();
        var clicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);

        // 按钮圆角 6（明显小于悬浮条本身的 10）
        var rounding = ModernButtonRounding;
        drawList.AddRectFilled(pos, max,
            SimplePalette.ToU32(hovered ? SimplePalette.FrameBgActive : SimplePalette.FrameBg), rounding);
        drawList.AddRect(pos, max,
            SimplePalette.ToU32(hovered ? SimplePalette.BorderStrong : SimplePalette.Border), rounding,
            ImDrawFlags.RoundCornersAll, 1.2f);

        var label = "设置";
        var textSize = ImGui.CalcTextSize(label);
        drawList.AddText(pos + (size - textSize) * 0.5f,
            SimplePalette.ToU32(SimplePalette.TextPrimary), label);

        if (hovered)
            ImGui.SetTooltip("打开 / 关闭 ACR 设置");
        if (clicked)
            toggleSettings();
    }
}
