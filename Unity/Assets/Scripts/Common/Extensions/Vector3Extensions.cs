using UnityEngine;

public static class Vector3Extensions
{
    public static Vector3 ToXZ(this Vector3 @this)
    {
        return new Vector3(@this.x, 0.0f, @this.z);
    }
}
