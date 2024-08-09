using System;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Refitter;

public static class Constants
{
    public unsafe delegate void CharaViewRenderDelegate(CharaView* view, uint index);

    public delegate nint RenderDelegate(nint a1, nint a2, int a3, int a4);

    /// See https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/Misc/CharaView.cs
    public const String CharaViewRenderSignature = "E8 ?? ?? ?? ?? 49 8B 4C 24 ?? 8B 51 04";

    /// Called sometime during the render loop
    public const String RenderSignature = "E8 ?? ?? ?? ?? 48 81 C3 ?? ?? ?? ?? BF ?? ?? ?? ?? 33 ED";
}
