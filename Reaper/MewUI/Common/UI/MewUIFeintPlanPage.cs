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
        SettingRow.SectionTitle("Mew 牵制规划");

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
        ImGui.TextColored(SimplePalette.TextSecondary,
            zones.Contains(currentZoneId)
                ? $"当前副本：{MewFeintPlanManager.GetZoneName(currentZoneId)}"
                : $"当前区域 ID：{currentZoneId}");
        ImGui.Spacing();

        foreach (var zoneId in zones)
        {
            if (zoneId != zones[0]) ImGui.SameLine();
            var selected = zoneId == selectedZoneId;
            if (selected)
                ImGui.PushStyleColor(ImGuiCol.Button, SimplePalette.NavActiveBg);
            if (ImGui.Button($"{MewFeintPlanManager.GetZoneName(zoneId)}##mew_feint_zone_{zoneId}"))
                selectedZoneId = zoneId;
            if (selected) ImGui.PopStyleColor();
        }

        DrawZone(settings, selectedZoneId);
        if (settings.ShowDebugInfo)
            DrawDebugLog();
    }

    private static void DrawZone(MewFeintPlanSettings settings, uint zoneId)
    {
        var plan = settings.GetOrCreateZonePlan(zoneId);
        MewFeintPlanManager.CleanupInvalidNodeIds(zoneId, plan);
        var phases = MewFeintPlanManager.GetZonePhases(zoneId);
        var total = phases.Sum(phase => MewFeintPlanManager.GetPhaseNodes(zoneId, phase).Count);

        ImGui.Spacing();
        SettingRow.SectionTitle($"{MewFeintPlanManager.GetZoneName(zoneId)}  ·  {plan.EnabledNodes.Count}/{total}");
        var enabled = plan.Enabled;
        if (SettingRow.Checkbox("启用此副本", ref enabled))
        {
            plan.Enabled = enabled;
            AcrSettingsStore.Save();
        }

        var presets = FeintPresets.GetPresets(zoneId);
        if (presets.Count > 0)
        {
            ImGui.Spacing();
            ImGui.TextColored(SimplePalette.TextSecondary, "快速预设");
            ImGui.SameLine();
            foreach (var preset in presets)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, SimplePalette.FrameBg);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SimplePalette.FrameBgHovered);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, SimplePalette.FrameBgActive);
                if (ImGui.Button($"{preset.Name}##mew_feint_preset_{zoneId}_{preset.Name}"))
                {
                    plan.EnabledNodes.Clear();
                    plan.EnabledNodes.AddRange(MewFeintPlanManager.ConvertPresetNodesToKeys(zoneId, preset));
                    plan.Enabled = true;
                    AcrSettingsStore.Save();
                }
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                {
                    var names = preset.Nodes.Concat(preset.PhaseNodes.Select(node =>
                        string.IsNullOrWhiteSpace(node.Phase) ? node.Name : $"{node.Phase} {node.Name}"));
                    ImGui.SetTooltip($"包含 {preset.Nodes.Count + preset.PhaseNodes.Count} 个节点\n{string.Join("\n", names)}");
                }
                ImGui.SameLine();
            }

            if (ImGui.Button($"清空##mew_feint_clear_{zoneId}"))
            {
                plan.EnabledNodes.Clear();
                AcrSettingsStore.Save();
            }
        }

        ImGui.Spacing();
        ImGui.BeginChild($"##mew_feint_nodes_{zoneId}", new Vector2(0f, 0f), false);
        foreach (var phase in phases)
        {
            var nodes = MewFeintPlanManager.GetPhaseNodes(zoneId, phase);
            if (nodes.Count == 0) continue;

            var phaseCount = nodes.Count(node => plan.IsNodeEnabled(phase, node.Id));
            ImGui.Spacing();
            ImGui.TextColored(SimplePalette.Accent,
                string.IsNullOrEmpty(phase) ? $"节点  {phaseCount}/{nodes.Count}" : $"{phase}  {phaseCount}/{nodes.Count}");
            ImGui.Separator();

            foreach (var node in nodes)
            {
                var checkedState = plan.IsNodeEnabled(phase, node.Id);
                ImGui.PushID($"{zoneId}_{phase}_{node.Id}");
                if (ImGui.Checkbox(node.Name, ref checkedState))
                {
                    plan.SetNodeEnabled(phase, node.Id, checkedState);
                    AcrSettingsStore.Save();
                }
                ImGui.PopID();
            }
        }
        ImGui.EndChild();
    }

    private static void DrawDebugLog()
    {
        if (!ImGui.CollapsingHeader("调试信息"))
            return;

        foreach (var message in MewFeintPlanDebug.RecentMessages.Reverse())
            ImGui.TextColored(SimplePalette.TextSecondary, message);
    }
}
