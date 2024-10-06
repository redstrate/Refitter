using System;

namespace Refitter;

public static class Constants
{
    /// The command name to open the config window.
    public const string CommandName = "/refitter";

    /// Function signature for RenderSignature
    public delegate nint RenderDelegate(nint a1, nint a2, int a3, int a4);

    /// Called sometime during the render loop
    public const String RenderSignature = "E8 ?? ?? ?? ?? 48 81 C3 ?? ?? ?? ?? BF ?? ?? ?? ?? 33 ED";

    /// Name of the left breast bone.
    public const String LeftBreastBoneName = "j_mune_l";
    
    /// Name of the right breast bone.
    public const String RightBreastBoneName = "j_mune_r";
    
    /// Name of the spine bone.
    public const String SpineBoneName = "j_sebo_b";
}
