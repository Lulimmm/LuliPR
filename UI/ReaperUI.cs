using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using PromeRotation.UI.HotKey;

namespace Reaper.PR;

public sealed partial class ReaperRotation
{
public void DrawSettings()
    {
        EnforceHiddenPanels();
        if (!ImGui.BeginTabBar("LuliReaperSettingsTabs")) return;
        if (ImGui.BeginTabItem("通用"))
        {
            ImGui.TextUnformatted("起手设置");
            ImGui.SetNextItemWidth(MathF.Max(180f, ImGui.GetContentRegionAvail().X));
            if (ImGui.BeginCombo("选择起手", selectedOpener))
            {
                foreach (var option in OpenerOptions)
                {
                    var isSelected = option == selectedOpener;
                    if (ImGui.Selectable(option, isSelected))
                    {
                        selectedOpener = option;
                        openerConfig.Selected = selectedOpener;
                        SaveOpenerConfig();
                    }
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGui.TextDisabled(selectedOpener == FollowTimelineOpener
                ? "不执行起手序列"
                : "使用此 ACR 的固定起手序列");
            DrawOpenerOptions();
            if (ImGui.CollapsingHeader("红玩设置"))
            {
                var extraRange = openerConfig.ExtraRange;
                if (ImGui.SliderFloat("额外攻击范围", ref extraRange, 0f, 3f, "%.1f 米"))
                {
                    openerConfig.ExtraRange = Math.Clamp(extraRange, 0f, 3f);
                    SaveOpenerConfig();
                }
                var moveCasting = openerConfig.MoveCasting;
                if (ImGui.SliderFloat("移动读条提前结束", ref moveCasting, 0f, 0.5f, "%.2f 秒"))
                {
                    openerConfig.MoveCasting = Math.Clamp(moveCasting, 0f, 0.5f);
                    SaveOpenerConfig();
                }
            }
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("说明书"))
        {
            ImGui.TextWrapped("附体状态下移动时会优先保留团契；长臂猿和移动读条选项由本 ACR 的战斗逻辑读取。起手药水与时间轴起手可分别配置。");
            ImGui.TextWrapped("自动真北只在身位要求和爆发窗口满足时插入，热键面板中的动作会进入 PromeRotation 高优先级队列。");
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Dev"))
        {
            ImGui.Text($"GCD: {ActionHelper.GetGcdTotal():F2}s");
            ImGui.Text($"死亡烙印: {R.TargetHasAny(2586u)}");
            ImGui.Text($"大丰收最近使用: {R.Recently(R.PlentifulHarvest, 1000)}");
            ImGui.Text($"灵魂值: {JobGaugeHelper.RPR.灵魂值}");
            ImGui.Text($"魂衣值: {JobGaugeHelper.RPR.魂衣值}");
            ImGui.EndTabItem();
        }
        ImGui.EndTabBar();
    }
    private void DrawOpenerOptions()
    {
        bool burst = openerConfig.OpenerBurst;
        if (ImGui.Checkbox("起手使用爆发药", ref burst)) { openerConfig.OpenerBurst = burst; SaveOpenerConfig(); }
        bool rush = openerConfig.OpenerRush;
        if (ImGui.Checkbox("起手使用突进", ref rush)) { openerConfig.OpenerRush = rush; SaveOpenerConfig(); }
        ImGui.SetNextItemWidth(180f);
        string potion = openerConfig.PotionType switch { 1 => "0510", 2 => "2814", _ => "0612" };
        if (ImGui.BeginCombo("爆发药策略", potion))
        {
            foreach (var item in new[] { ("0612", 0), ("0510", 1), ("2814", 2) })
            {
                if (ImGui.Selectable(item.Item1, item.Item2 == openerConfig.PotionType)) { openerConfig.PotionType = item.Item2; SaveOpenerConfig(); }
            }
            ImGui.EndCombo();
        }
        bool hideQtPage = openerConfig.HideQtPage;
        if (ImGui.Checkbox("隐藏 QT 浮窗", ref hideQtPage))
        {
            openerConfig.HideQtPage = hideQtPage;
            SaveOpenerConfig();
            EnforceHiddenPanels();
        }
        bool hideHotkeyPage = openerConfig.HideHotkeyPage;
        if (ImGui.Checkbox("隐藏 HOTKEY 面板", ref hideHotkeyPage))
        {
            openerConfig.HideHotkeyPage = hideHotkeyPage;
            SaveOpenerConfig();
            if (hideHotkeyPage) hotkeyPanel.Close();
            else hotkeyPanel.Open();
            EnforceHiddenPanels();
        }
    }

    private void EnforceHiddenPanels()
    {
        SetQtPageHidden(openerConfig.HideQtPage);
        if (openerConfig.HideHotkeyPage && hotkeyPanel.IsOpen)
            hotkeyPanel.Close();
    }

    private void SetQtPageHidden(bool hidden)
    {
        foreach (var id in qtIds)
        {
            if (hidden)
            {
                if (PromeSettings.Instance.HiddenQts.Add(id)) hiddenQtIds.Add(id);
            }
            else if (hiddenQtIds.Remove(id))
            {
                PromeSettings.Instance.HiddenQts.Remove(id);
            }
        }
    }
    public void DrawQTs() { }

    private sealed class DynamicActionLogic : IHotkeyLogic
    {
        private readonly Func<uint> actionId;
        private readonly ActionType type;
        private readonly ActionTargetType target;

        public DynamicActionLogic(Func<uint> actionId, ActionType type, ActionTargetType target)
        {
            this.actionId = actionId;
            this.type = type;
            this.target = target;
        }

        public bool IsActive() => false;

        public void OnClick()
        {
            var id = actionId();
            if (id != 0) HotkeyQueueManager.TryEnqueue(new PAction(id, type, target) { RequiresVerification = true });
        }
    }
}
