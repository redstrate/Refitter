using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;

namespace Refitter;

public sealed class Plugin : IDalamudPlugin
{
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

    /// Called sometime during the render loop
    [Signature("E8 ?? ?? ?? ?? 48 81 C3 ?? ?? ?? ?? BF ?? ?? ?? ?? 33 ED", DetourName = nameof(Render))]
    private readonly Hook<RenderDelegate>? renderHook = null!;

    public readonly WindowSystem WindowSystem = new("Refitter");

    public bool EnableOverrides = true;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Hooking.InitializeFromAttributes(this);

        renderHook?.Enable();

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

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
    }

    private nint Render(nint a1, nint a2, int a3, int a4)
    {
        if (EnableOverrides) ApplyArmature();
        return renderHook!.Original(a1, a2, a3, a4);
    }

    private unsafe void ApplyArmature()
    {
        var localPlayer = ClientState.LocalPlayer;
        if (localPlayer == null) return;

        var gameObject = (Character*)localPlayer.Address;
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
}
