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
        ImGui.Checkbox("Enable Adjustments", ref plugin.EnableOverrides);

        unsafe
        {
            var localPlayer = Plugin.ClientState.LocalPlayer;
            if (localPlayer == null) return;

            var gameObject = (Character*)localPlayer.Address;
            if (gameObject == null) return;

            var torsoData = gameObject->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body);
            ImGui.Text($"Current Torso ID: {torsoData.Id}");

            ImGui.Separator();

            ImGui.TextDisabled("Adjustments");

            if (ImGui.Button("Add Override"))
            {
                var configModel = new ConfigModel
                {
                    Model = torsoData.Id
                };
                Plugin.Configuration.Configs.Add(configModel);
            }

            if (ImGui.Button("Save")) Plugin.Configuration.Save();

            var previewSmallClothes = plugin.PreviewSmallclothes;
            if (ImGui.Checkbox("Preview Smallclothes", ref previewSmallClothes))
                plugin.PreviewSmallclothes = previewSmallClothes;

            ImGui.BeginChild("overrides", new Vector2(-1, -1), true);

            var adjustment = Plugin.Configuration.Configs.Find(x => x.Model == torsoData.Id);
            if (adjustment != null)
            {
                ImGui.Text("Model Entry");

                ImGui.DragFloat("Gravity", ref adjustment.Gravity, 0.001f);
                ImGui.DragFloat3("Scale Override", ref adjustment.NewScale, 0.001f);
                ImGui.DragFloat3("Pos Override", ref adjustment.NewPos, 0.001f);
                ImGui.DragFloat("Push Down", ref adjustment.PushDown, 0.001f);
                ImGui.DragFloat("Push Up", ref adjustment.PushUp, 0.001f);

                ImGui.Separator();
            }
            else
                ImGui.TextDisabled("No adjustment for this torso item.");

            ImGui.EndChild();
        }
    }
}
