using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using LuliPR.Common;
using Mew.FeintPlan;

namespace MewUI;

internal static class MewUIFeintPlanPage
{
    private static uint selectedZoneId;
    private static uint lastObservedZoneId;

    public static void Draw()
    {
        var settings = AcrSettingsStore.Current.MewFeintPlan;
        SettingRow.SectionTitle("牵制规划");

        var globallyEnabled = settings.GlobalEnabled;
        if (SettingRow.Checkbox("启用牵制规划", ref globallyEnabled))
        {
            settings.GlobalEnabled = globallyEnabled;
            AcrSettingsStore.Save();
        }

        ImGui.SameLine();
        var showDebug = settings.ShowDebugInfo;
        if (SettingRow.Checkbox("调试记录", ref showDebug))
        {
            settings.ShowDebugInfo = showDebug;
            AcrSettingsStore.Save();
        }

        var zones = MewFeintPlanManager.GetAllZoneIds();
        if (zones.Count == 0)
        {
            ImGui.TextColored(SimplePalette.TextSecondary, "没有可用的牵制节点");
            return;
        }

        var currentZoneId = (uint)MewFeintPlanManager.CurrentZoneId;
        if (currentZoneId != lastObservedZoneId)
        {
            lastObservedZoneId = currentZoneId;
            if (zones.Contains(currentZoneId))
                selectedZoneId = currentZoneId;
        }
        if (!zones.Contains(selectedZoneId))
            selectedZoneId = zones[0];

        ImGui.Spacing();
        ImGui.TextColored(SimplePalette.TextSecondary, "副本");
        ImGui.SameLine();
        ImGui.TextColored(SimplePalette.Accent,
            zones.Contains(currentZoneId)
                ? $"当前：{MewFeintPlanManager.GetZoneName(currentZoneId)}"
                : $"当前区域 ID：{currentZoneId}");

        var savageZones = new[]
        {
            FeintZoneConfig.ZoneIds["M9S"],
            FeintZoneConfig.ZoneIds["M10S"],
            FeintZoneConfig.ZoneIds["M11S"],
            FeintZoneConfig.ZoneIds["M12S"]
        }.Where(zones.Contains).ToArray();
        var ultimateZones = new[]
        {
            FeintZoneConfig.ZoneIds["FRU"],
            FeintZoneConfig.ZoneIds["TOP"],
            FeintZoneConfig.ZoneIds["DMU"]
        }.Where(zones.Contains).ToArray();

        DrawZoneGroup("零式副本", savageZones);
        DrawZoneGroup("绝境战", ultimateZones);

        DrawZone(settings, selectedZoneId);
        if (settings.ShowDebugInfo)
            DrawDebugLog();
    }

    private static void DrawZoneGroup(string label, IReadOnlyList<uint> zoneIds)
    {
        if (zoneIds.Count == 0) return;

        ImGui.Spacing();
        ImGui.TextColored(SimplePalette.TextSecondary, label);
        if (ImGui.BeginTable($"##mew_feint_zones_{label}", 4, ImGuiTableFlags.SizingStretchSame))
        {
            foreach (var zoneId in zoneIds)
            {
                ImGui.TableNextColumn();
                DrawChoiceButton(
                    MewFeintPlanManager.GetZoneName(zoneId),
                    $"##mew_feint_zone_{zoneId}",
                    zoneId == selectedZoneId,
                    () => selectedZoneId = zoneId,
                    height: 26f);
            }
            ImGui.EndTable();
        }
    }

    private static void DrawZone(MewFeintPlanSettings settings, uint zoneId)
    {
        var plan = settings.GetOrCreateZonePlan(zoneId);
        MewFeintPlanManager.CleanupInvalidNodeIds(zoneId, plan);
        var phases = MewFeintPlanManager.GetZonePhases(zoneId);
        var total = phases.Sum(phase => MewFeintPlanManager.GetPhaseNodes(zoneId, phase).Count);

        ImGui.Spacing();
        SettingRow.SectionTitle($"{MewFeintPlanManager.GetZoneName(zoneId)} 减伤");
        var enabled = plan.Enabled;
        if (SettingRow.Checkbox("启用此副本", ref enabled))
        {
            plan.Enabled = enabled;
            AcrSettingsStore.Save();
        }
        ImGui.SameLine();
        ImGui.TextColored(SimplePalette.TextSecondary,
            $"已选 {phases.Sum(phase => MewFeintPlanManager.GetPhaseNodes(zoneId, phase)
                .Count(node => plan.IsNodeEnabled(phase, node.Id)))}/{total}");

        var presets = FeintPresets.GetPresets(zoneId);
        if (presets.Count > 0)
        {
            ImGui.Spacing();
            ImGui.TextColored(SimplePalette.TextSecondary, "减伤预设");
            foreach (var preset in presets)
            {
                ImGui.SameLine();
                var nodeKeys = MewFeintPlanManager.ConvertPresetNodesToKeys(zoneId, preset);
                var selected = plan.Enabled
                    && plan.EnabledNodes.SequenceEqual(nodeKeys, StringComparer.Ordinal);
                DrawChoiceButton(preset.Name, $"##mew_feint_preset_{zoneId}_{preset.Name}", selected, () =>
                {
                    plan.EnabledNodes.Clear();
                    plan.EnabledNodes.AddRange(nodeKeys);
                    plan.Enabled = true;
                    AcrSettingsStore.Save();
                }, 68f);
                if (ImGui.IsItemHovered())
                {
                    var names = preset.Nodes.Concat(preset.PhaseNodes.Select(node =>
                        string.IsNullOrWhiteSpace(node.Phase) ? node.Name : $"{node.Phase} {node.Name}"));
                    ImGui.SetTooltip($"包含 {preset.Nodes.Count + preset.PhaseNodes.Count} 个节点\n{string.Join("\n", names)}");
                }
            }

            ImGui.SameLine();
            DrawChoiceButton("清空", $"##mew_feint_clear_{zoneId}", false, () =>
            {
                plan.EnabledNodes.Clear();
                AcrSettingsStore.Save();
            }, 68f, secondary: true);
        }

        ImGui.Spacing();
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 7f));
        ImGui.BeginChild($"##mew_feint_nodes_{zoneId}", new Vector2(0f, 0f), false);
        foreach (var phase in phases)
        {
            var nodes = MewFeintPlanManager.GetPhaseNodes(zoneId, phase);
            if (nodes.Count == 0) continue;

            var phaseCount = nodes.Count(node => plan.IsNodeEnabled(phase, node.Id));
            ImGui.Spacing();
            ImGui.TextColored(SimplePalette.Accent, string.IsNullOrEmpty(phase) ? "节点" : phase);
            ImGui.SameLine();
            ImGui.TextColored(SimplePalette.TextSecondary, $"{phaseCount}/{nodes.Count}");
            ImGui.Separator();

            foreach (var node in nodes)
            {
                var checkedState = plan.IsNodeEnabled(phase, node.Id);
                ImGui.PushID($"{zoneId}_{phase}_{node.Id}");
                var label = node.EnabledByDefault ? node.Name : $"{node.Name}（用户自选）";
                if (ImGui.Checkbox(label, ref checkedState))
                {
                    plan.SetNodeEnabled(phase, node.Id, checkedState);
                    AcrSettingsStore.Save();
                }
                if (ImGui.IsItemHovered() && !string.IsNullOrWhiteSpace(node.Description))
                    ImGui.SetTooltip(node.Description);
                ImGui.PopID();
            }
        }
        ImGui.EndChild();
        ImGui.PopStyleVar();
    }

    private static void DrawChoiceButton(
        string label,
        string id,
        bool selected,
        Action onClick,
        float width = -1f,
        bool secondary = false,
        float height = 30f)
    {
        var background = selected
            ? SimplePalette.NavActiveBg
            : secondary ? SimplePalette.WithAlpha(SimplePalette.FrameBg, 0.58f) : SimplePalette.FrameBg;
        ImGui.PushStyleColor(ImGuiCol.Button, background);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SimplePalette.FrameBgHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, SimplePalette.FrameBgActive);
        ImGui.PushStyleColor(ImGuiCol.Border, selected ? SimplePalette.PrimaryHover : SimplePalette.Border);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, selected ? 1f : secondary ? 0.5f : 0f);
        var buttonWidth = width < 0f ? ImGui.GetContentRegionAvail().X : width;
        if (ImGui.Button(label + id, new Vector2(buttonWidth, height)))
            onClick();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor(4);
    }

    private static void DrawDebugLog()
    {
        if (!ImGui.CollapsingHeader("调试信息"))
            return;

        ImGui.TextColored(SimplePalette.TextSecondary,
            $"PR LogSystem：{(MewFeintPlanManager.EventSourceConnected ? "已连接" : "未连接，正在重试")}");
        ImGui.TextColored(SimplePalette.TextSecondary,
            $"读条事件 {MewFeintPlanManager.ReceivedCastCount}（最近 {MewFeintPlanManager.LastCastActionId}）  " +
            $"技能效果 {MewFeintPlanManager.ReceivedEffectCount}（最近 {MewFeintPlanManager.LastEffectActionId}）");
        foreach (var status in MewFeintPlanManager.GetPendingDebugLines())
            ImGui.TextWrapped(status);
        ImGui.Separator();
        foreach (var message in MewFeintPlanDebug.RecentMessages.Reverse())
            ImGui.TextColored(SimplePalette.TextSecondary, message);
    }
}
