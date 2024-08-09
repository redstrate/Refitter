using System;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Refitter;

public static class Constants
{
    /// The maximum number of CharaView's to adjust for.
    // TODO: is this actually the maximum number of possible character views on screen?
    public const int MaxCharaViews = 5;

    /// The command name to open the config window.
    public const string CommandName = "/refitter";
    
    // NOTE: See https://github.com/Ottermandias/Penumbra.GameData/blob/016da3c2219a3dbe4c2841ae0d1305ae0b2ad60f/Enums/ScreenActor.cs
    /// The start index in the gameobject list for cutscene actors.
    public const int CutsceneStart = 200;
    /// The end index in the gameobject list for cutscene actors. 
    public const int CutsceneEnd = 250;
    
    /// Function signature for CharaViewRenderSignature
    public unsafe delegate void CharaViewRenderDelegate(CharaView* view, uint index);

    /// Function signature for RenderSignature
    public delegate nint RenderDelegate(nint a1, nint a2, int a3, int a4);

    /// The signature for CharaView::Render()
    // NOTE: See https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/Misc/CharaView.cs
    public const String CharaViewRenderSignature = "E8 ?? ?? ?? ?? 49 8B 4C 24 ?? 8B 51 04";

    /// Called sometime during the render loop
    public const String RenderSignature = "E8 ?? ?? ?? ?? 48 81 C3 ?? ?? ?? ?? BF ?? ?? ?? ?? 33 ED";
}
