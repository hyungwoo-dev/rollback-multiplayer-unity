using FixedMathSharp;
using UnityEngine;

public static class MathUtils
{

    public static Vector3d ToXZ(this Vector3d vec)
    {
        return new Vector3d(vec.x, Fixed64.Zero, vec.z);
    }

    public static Vector3 ToVector3(this Vector3d vec)
    {
        return new Vector3(vec.x.ToPreciseFloat(), vec.y.ToPreciseFloat(), vec.z.ToPreciseFloat());
    }

    public static Vector3d ToVector3d(this Vector3 vec)
    {
        return new Vector3d(vec.x, vec.y, vec.z);
    }

    public static FixedQuaternion ToFixedQuaternion(this Quaternion quat)
    {
        return new FixedQuaternion(
            (Fixed64)quat.x,
            (Fixed64)quat.y,
            (Fixed64)quat.z,
            (Fixed64)quat.w
        );
    }

    public static Quaternion ToQuaternion(this FixedQuaternion quat)
    {
        return new Quaternion(
            (float)quat.x,
            (float)quat.y,
            (float)quat.z,
            (float)quat.w
        );
    }

    public static Fixed64 Min(Fixed64 a, Fixed64 b)
    {
        return a < b ? a : b;
    }

    public static Fixed64 Max(Fixed64 a, Fixed64 b)
    {
        return a > b ? a : b;
    }

    public static long Max(long a, long b)
    {
        return a > b ? a : b;
    }
}
