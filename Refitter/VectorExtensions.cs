using System.Numerics;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;

namespace Refitter;

/// Helper methods to convert between Quaternions
internal static class VectorExtensions
{
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
