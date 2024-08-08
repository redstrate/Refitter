﻿using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;

namespace Refitter;

public sealed class Plugin : IDalamudPlugin
{
    // TODO: is this actually the maximum number of possible character views on screen?
    private const int MaxCharaViews = 5;
    public static Configuration Configuration = null!;

    /// <summary>
    ///     A "null" havok vector. Since the type isn't inherently nullable, and the default value (0, 0, 0, 0)
    ///     is valid input in a lot of cases, we can use this instead.
    /// </summary>
    public static readonly hkVector4f NullVector = new()
    {
        X = float.NaN,
        Y = float.NaN,
        Z = float.NaN,
        W = float.NaN
    };

    /// <summary>
    ///     A "null" havok quaternion. Since the type isn't inherently nullable, and the default value (0, 0, 0, 0)
    ///     is valid input in a lot of cases, we can use this instead.
    /// </summary>
    public static readonly hkQuaternionf NullQuaternion = new()
    {
        X = float.NaN,
        Y = float.NaN,
        Z = float.NaN,
        W = float.NaN
    };

    /// <summary>
    ///     A "null" havok transform. Since the type isn't inherently nullable, and the default values
    ///     aren't immediately obviously wrong, we can use this instead.
    /// </summary>
    public static readonly hkQsTransformf NullTransform = new()
    {
        Translation = NullVector,
        Rotation = NullQuaternion,
        Scale = NullVector
    };

    /// See https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/Misc/CharaView.cs
    [Signature("E8 ?? ?? ?? ?? 49 8B 4C 24 ?? 8B 51 04", DetourName = nameof(RenderCharaView))]
    private readonly Hook<CharaViewRenderDelegate>? charaViewRenderHook = null!;

    private readonly unsafe CharaView*[] charaViews = new CharaView*[MaxCharaViews];

    /// Called sometime during the render loop
    [Signature("E8 ?? ?? ?? ?? 48 81 C3 ?? ?? ?? ?? BF ?? ?? ?? ?? 33 ED", DetourName = nameof(Render))]
    private readonly Hook<RenderDelegate>? renderHook = null!;

    public readonly WindowSystem WindowSystem = new("Refitter");

    public bool EnableOverrides = true;
    private int numCharaViews;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Hooking.InitializeFromAttributes(this);

        renderHook?.Enable();
        charaViewRenderHook?.Enable();

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ConfigWindow.Toggle;
    }

    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static IGameInteropProvider Hooking { get; private set; } = null!;

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    private ConfigWindow ConfigWindow { get; init; }

    public void Dispose()
    {
        renderHook?.Dispose();
        charaViewRenderHook?.Dispose();

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
    }

    private nint Render(nint a1, nint a2, int a3, int a4)
    {
        unsafe
        {
            if (EnableOverrides)
            {
                var localPlayer = ClientState.LocalPlayer;
                if (localPlayer != null)
                {
                    var gameObject = (Character*)localPlayer.Address;
                    if (gameObject != null) ApplyArmature(gameObject);
                }
            }
        }

        unsafe
        {
            for (var i = 0; i < numCharaViews; i++) ApplyArmature(charaViews[i]->GetCharacter());
        }

        numCharaViews = 0;

        return renderHook!.Original(a1, a2, a3, a4);
    }

    private unsafe void RenderCharaView(CharaView* view, uint index)
    {
        // Okay, this looks a little weird. Why not apply the armature here?
        // For some reason I don't understand, this doesn't work when doing it at this time.
        // I assume because this is mistakenly called "Render" but this seems more setup-y.
        // So we'll just append it to the list instead, and handle it in the main render function.
        if (EnableOverrides)
        {
            if (view->GetCharacter() != null && numCharaViews + 1 < MaxCharaViews)
            {
                charaViews[numCharaViews] = view;
                numCharaViews++;
            }
        }

        charaViewRenderHook!.Original(view, index);
    }

    private unsafe void ApplyArmature(Character* gameObject)
    {
        if (gameObject == null) return;

        var torsoData = gameObject->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body);

        var charBase = (CharacterBase*)gameObject->DrawObject;
        if (charBase == null) return;

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
                            if (boneName is "j_mune_l" or "j_mune_r")
                            {
                                var existingTransform = currentPose->ModelPose[boneIndex];

                                var modelOverride = Configuration.GetModelOverride(torsoData.Id);
                                if (modelOverride != null)
                                {
                                    existingTransform.Scale.X *=
                                        modelOverride.NewScale.X;
                                    existingTransform.Scale.Y *= modelOverride.NewScale.Y;
                                    existingTransform.Scale.Z *=
                                        modelOverride.NewScale.Z + (modelOverride.Gravity * 2.5f);

                                    existingTransform.Translation.Z += modelOverride.Gravity * 0.25f;
                                    existingTransform.Translation.Y -= modelOverride.Gravity * 1.1f;

                                    existingTransform.Translation.X += modelOverride.NewPos.X;
                                    existingTransform.Translation.Y += modelOverride.NewPos.Y;
                                    existingTransform.Translation.Z -= modelOverride.NewPos.Z;

                                    var rotation = new Vector3();
                                    rotation.Z = modelOverride.Gravity * 350;

                                    if (boneName == "j_mune_l")
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
                            if (boneName == "j_sebo_b")
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

    /// The detour function signature
    private delegate nint RenderDelegate(nint a1, nint a2, int a3, int a4);

    /// Chara View detour function signature
    private unsafe delegate void CharaViewRenderDelegate(CharaView* view, uint index);
}
