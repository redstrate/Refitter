using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using Newtonsoft.Json;

namespace Refitter;

public class ConfigWindow(Plugin plugin)
    : Window("Refitter Configuration"), IDisposable
{
    public void Dispose() { }

    public override void Draw()
    {
        unsafe
        {
            var localPlayer = Plugin.ClientState.LocalPlayer;
            if (localPlayer == null) return;

            var gameObject = (Character*)localPlayer.Address;
            if (gameObject == null) return;

            var torsoData = gameObject->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body);
            
            if (ImGui.BeginTabBar("tabbar"))
            {
                if (ImGui.BeginTabItem("Adjustments"))
                {
                    ImGui.Checkbox("Enable Adjustments", ref plugin.EnableOverrides);

                    var previewSmallClothes = plugin.PreviewSmallclothes;
                    if (ImGui.Checkbox("Preview Smallclothes", ref previewSmallClothes))
                        plugin.PreviewSmallclothes = previewSmallClothes;
                    
                    ImGui.Separator();
                    
                    if (previewSmallClothes)
                        ImGui.TextDisabled("Cannot adjust while previewing smallclothes.");
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

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Data"))
                {
                    ImGui.TextWrapped("This page is currently a work-in-progress. These buttons do NOT prompt for confirmation, so make sure to backup your data first!");
                    
                    ImGui.TextDisabled("Import from JSON");
                    ImGui.Separator();
                    if (ImGui.Button("Import to Clipboard"))
                    {
                        string clipboardText = ImGui.GetClipboardText();
                        ImGui.OpenPopup("import");

                        try
                        {
                            List<ConfigModel>? m = JsonConvert.DeserializeObject<List<ConfigModel>>(clipboardText);
                            if (m != null)
                            {
                                Plugin.Configuration.Configs = m;
                                Plugin.Configuration.Save();

                                var notification = new Notification
                                {
                                    Content = "Successfully imported data."
                                };

                                Plugin.NotificationManager.AddNotification(notification);
                            }
                        }
                        catch
                        {
                            var notification = new Notification
                            {
                                Content = "Failed to import data."
                            };

                            Plugin.NotificationManager.AddNotification(notification);
                        }
                    }
                    
                    ImGui.TextDisabled("Export as JSON");
                    ImGui.Separator();
                    if (ImGui.Button("Copy to Clipboard"))
                    {
                        ImGui.SetClipboardText(JsonConvert.SerializeObject(Plugin.Configuration.Configs, Formatting.Indented));
                    }
                    
                    ImGui.TextDisabled("Reset");
                    ImGui.Separator();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                    if (ImGui.Button("Reset"))
                    {
                        Plugin.Configuration.Configs.Clear();
                        Plugin.Configuration.Save();
                    }
                    ImGui.PopStyleColor(1);
                    
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
    }
}
