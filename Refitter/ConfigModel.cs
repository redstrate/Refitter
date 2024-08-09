using System;
using System.Numerics;

namespace Refitter;

[Serializable]
public class ConfigModel
{
    public float Gravity = 0.0f;

    // The model ID
    public uint Model;

    public Vector3 NewPos;

    // The new scale of the chest starting at 0 (it's added on top of the existing transform)
    public Vector3 NewScale = Vector3.One;

    public float PushDown = 0.0f;
    public float PushUp = 0.0f;
}
