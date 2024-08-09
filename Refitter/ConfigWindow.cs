using System;
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

            var previewSmallClothes = plugin.PreviewSmallclothes;
            if (ImGui.Checkbox("Preview Smallclothes", ref previewSmallClothes))
                plugin.PreviewSmallclothes = previewSmallClothes;
            
            ImGui.Separator();

            if (previewSmallClothes)
            {
                ImGui.TextDisabled("Cannot adjust while previewing smallclothes.");
            }
            else
            {
                var adjustment = Plugin.Configuration.Configs.Find(x => x.Model == torsoData.Id);
                if (adjustment != null)
                {
                    ImGui.TextDisabled($"Adjustments for {torsoData.Id}");

                    ImGui.DragFloat("Gravity", ref adjustment.Gravity, 0.001f);
                    ImGui.DragFloat3("Scale Override", ref adjustment.NewScale, 0.001f);
                    ImGui.DragFloat3("Pos Override", ref adjustment.NewPos, 0.001f);
                    ImGui.DragFloat("Push Down", ref adjustment.PushDown, 0.001f);
                    ImGui.DragFloat("Push Up", ref adjustment.PushUp, 0.001f);

                    if (ImGui.Button("Save")) Plugin.Configuration.Save();
                }
                else
                {
                    ImGui.TextDisabled("No adjustment for this torso item.");

                    if (ImGui.Button("Adjust"))
                    {
                        var configModel = new ConfigModel
                        {
                            Model = torsoData.Id
                        };
                        Plugin.Configuration.Configs.Add(configModel);
                    }
                }
            }
        }
    }
}
