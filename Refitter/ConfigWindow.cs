using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;

namespace Refitter;

public class ConfigWindow(Plugin plugin)
    : Window("Refitter Configuration"), IDisposable
{
    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Checkbox("Enable Overrides", ref plugin.EnableOverrides);

        unsafe
        {
            var localPlayer = Plugin.ClientState.LocalPlayer;
            if (localPlayer == null) return;

            var gameObject = (Character*)localPlayer.Address;
            if (gameObject == null) return;

            var torsoData = gameObject->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body);
            ImGui.Text($"Current Torso ID: {torsoData.Id}");
        }
        
        ImGui.Separator();

        ImGui.TextDisabled("Overrides");

        if (ImGui.Button("Add Override")) Plugin.Configuration.Configs.Add(new ConfigModel());

        if (ImGui.Button("Save")) Plugin.Configuration.Save();

        ImGui.BeginChild("overrides", new Vector2(-1, -1), true);

        var i = 0;
        foreach (var config in Plugin.Configuration.Configs)
        {
            ImGui.PushID(i);
            ImGui.Text("Model Entry");
            var modelId = (int)config.Model;
            if (ImGui.InputInt("Model ID", ref modelId)) config.Model = (uint)modelId;

            ImGui.DragFloat("Gravity", ref config.Gravity, 0.001f);
            ImGui.DragFloat3("Scale Override", ref config.NewScale, 0.001f);
            ImGui.DragFloat("Push Down", ref config.PushDown, 0.001f);
            ImGui.DragFloat("Push Up", ref config.PushUp, 0.001f);

            ImGui.Separator();
            ImGui.PopID();

            i++;
        }

        ImGui.EndChild();
    }
}
