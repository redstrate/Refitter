using System.Numerics;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;

namespace Refitter;

/// Helper methods to convert between Quaternions
internal static class VectorExtensions
{
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

    /// Convert from Vector3 to Quaternion
    public static Quaternion ToQuaternion(this Vector3 rotation)
    {
        return Quaternion.CreateFromYawPitchRoll(
            float.DegreesToRadians(rotation.X),
            float.DegreesToRadians(rotation.Y),
            float.DegreesToRadians(rotation.Z));
    }

    /// Convert from Havok's Quaternion to System.Numeric's Quaternion
    public static Quaternion ToQuaternion(this hkQuaternionf rotation)
    {
        return new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
    }
}
