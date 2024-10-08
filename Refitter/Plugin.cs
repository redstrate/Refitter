﻿using System.Numerics;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Refitter;

public sealed class Plugin : IDalamudPlugin
{
    public static Configuration Configuration = null!;

    [Signature(Constants.RenderSignature, DetourName = nameof(Render))]
    private readonly Hook<Constants.RenderDelegate>? renderHook = null!;

    public readonly WindowSystem WindowSystem = new("Refitter");

    public bool EnableOverrides = true;

    private EquipmentModelId? oldTorsoEquipment;

    private bool previewSmallclothes;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Hooking.InitializeFromAttributes(this);

        renderHook?.Enable();

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(Constants.CommandName, new CommandInfo(OnConfigCommand)
        {
            HelpMessage = "Display the Refitter config window."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ConfigWindow.Toggle;
    }

    public bool PreviewSmallclothes
    {
        get => previewSmallclothes;
        set
        {
            previewSmallclothes = value;
            if (value) HideArmor();
            else RestoreArmor();
        }
    }

    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static IGameInteropProvider Hooking { get; private set; } = null!;

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;
    
    [PluginService]
    internal static INotificationManager NotificationManager { get; private set; } = null!;

    private ConfigWindow ConfigWindow { get; init; }

    public void Dispose()
    {
        PreviewSmallclothes = false;

        renderHook?.Dispose();

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
    }

    private void OnConfigCommand(string command, string arguments)
    {
        ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
    }

    private unsafe nint Render(nint a1, nint a2, int a3, int a4)
    {
        if (EnableOverrides)
        {
            foreach (var gameObject in GameObjectManager.Instance()->Objects.IndexSorted)
            {
                ApplyArmature((Character*)gameObject.Value);
            }
        }

        // If the player switches clothes, don't consider it previewed anymore
        if (PreviewSmallclothes && oldTorsoEquipment != null)
        {
            var localPlayer = ClientState.LocalPlayer;
            if (localPlayer != null)
            {
                var gameObject = (Character*)localPlayer.Address;
                if (gameObject != null)
                {
                    var torsoData = gameObject->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body);
                    if (!torsoData.Equals(oldTorsoEquipment.Value) && torsoData.Id != 0) previewSmallclothes = false;
                }
            }
        }

        return renderHook!.Original(a1, a2, a3, a4);
    }

    public unsafe void HideArmor()
    {
        var localPlayer = ClientState.LocalPlayer;
        if (localPlayer != null)
        {
            var gameObject = (Character*)localPlayer.Address;

            var torsoData = gameObject->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body);
            if (PreviewSmallclothes)
            {
                oldTorsoEquipment = torsoData;
                torsoData = new EquipmentModelId
                {
                    Id = 0
                };
                gameObject->DrawData.LoadEquipment(DrawDataContainer.EquipmentSlot.Body, &torsoData, false);
            }
        }
    }

    public unsafe void RestoreArmor()
    {
        if (oldTorsoEquipment == null) return;

        var localPlayer = ClientState.LocalPlayer;
        if (localPlayer != null)
        {
            var gameObject = (Character*)localPlayer.Address;
            var torsoData = oldTorsoEquipment.Value;
            gameObject->DrawData.LoadEquipment(DrawDataContainer.EquipmentSlot.Body, &torsoData, false);
        }
    }

    private unsafe void ApplyArmature(Character* gameObject)
    {
        if (gameObject == null) return;

        var torsoData = gameObject->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body);

        var charBase = (CharacterBase*)gameObject->DrawObject;
        if (charBase == null) return;

        var human = (Human*)gameObject->DrawObject;
        if (human == null) return;

        // Only apply to females
        if (human->Customize.Sex != 1) return;

        var applyAmount = human->Customize.BustSize / 100.0f;

        // Don't apply to Lalafells
        if (human->Customize.Race == 3) return;

        var skeleton = charBase->Skeleton;
        if (skeleton == null) return;

        // lol, sometimes this fails for some reason...?
        try
        {
            for (var pSkeleIndex = 0; pSkeleIndex < skeleton->PartialSkeletonCount; ++pSkeleIndex)
            {
                var currentPose = skeleton->PartialSkeletons[pSkeleIndex].GetHavokPose(0);
                if (currentPose != null)
                {
                    if (currentPose->Skeleton != null)
                    {
                        for (var boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Length; ++boneIndex)
                        {
                            var boneName = currentPose->Skeleton->Bones[boneIndex].Name.String;
                            if (boneName is Constants.LeftBreastBoneName or Constants.RightBreastBoneName)
                            {
                                var existingTransform = currentPose->ModelPose[boneIndex];

                                var modelOverride = Configuration.GetModelOverride(torsoData.Id);
                                if (modelOverride != null)
                                {
                                    existingTransform.Scale.X *=
                                        float.Lerp(1.0f, modelOverride.NewScale.X, applyAmount);
                                    existingTransform.Scale.Y *= float.Lerp(1.0f, modelOverride.NewScale.Y, applyAmount);
                                    existingTransform.Scale.Z *=
                                        float.Lerp(1.0f, modelOverride.NewScale.Z + (modelOverride.Gravity * 2.5f), applyAmount);

                                    existingTransform.Translation.Z += float.Lerp(0.0f, modelOverride.Gravity * 0.25f, applyAmount);
                                    existingTransform.Translation.Y -= float.Lerp(0.0f, modelOverride.Gravity * 1.1f, applyAmount);

                                    existingTransform.Translation.X += float.Lerp(0.0f, modelOverride.NewPos.X, applyAmount);
                                    existingTransform.Translation.Y += float.Lerp(0.0f, modelOverride.NewPos.Y, applyAmount);
                                    existingTransform.Translation.Z -= float.Lerp(0.0f, modelOverride.NewPos.Z, applyAmount);

                                    var rotation = new Vector3
                                    {
                                        Z = modelOverride.Gravity * 350
                                    };

                                    if (boneName == Constants.LeftBreastBoneName)
                                    {
                                        existingTransform.Translation.X -= modelOverride.PushUp * 1.25f;
                                        rotation.X = modelOverride.PushUp * -350;
                                    }
                                    else
                                    {
                                        existingTransform.Translation.X += modelOverride.PushUp * 1.25f;
                                        rotation.X = modelOverride.PushUp * 350;
                                    }

                                    var newRotation =
                                        Quaternion.Multiply(existingTransform.Rotation.ToQuaternion(),
                                                            rotation.ToQuaternion());
                                    existingTransform.Rotation.X = newRotation.X;
                                    existingTransform.Rotation.Y = newRotation.Y;
                                    existingTransform.Rotation.Z = newRotation.Z;
                                    existingTransform.Rotation.W = newRotation.W;
                                }

                                currentPose->ModelPose[boneIndex] = existingTransform;
                            }

                            // spine b
                            if (boneName == Constants.SpineBoneName)
                            {
                                var existingTransform = currentPose->ModelPose[boneIndex];

                                var modelOverride = Configuration.GetModelOverride(torsoData.Id);
                                if (modelOverride != null)
                                {
                                    existingTransform.Translation.Y -=
                                        (modelOverride.Gravity * 0.15f) + modelOverride.PushDown;
                                }

                                currentPose->ModelPose[boneIndex] = existingTransform;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // ignored
        }
    }
}
