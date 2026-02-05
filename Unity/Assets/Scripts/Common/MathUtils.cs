using UnityEngine;

public class MathUtils
{
    public static float Sqrt(float v, float epsilon = 0.0001f)
    {
        float x = 1.0f;
        var count = 0;
        while ((v - epsilon >= x * x) || (x * x >= v + epsilon))
        {
            x = (x + v / x) * 0.5f;
            count += 1;
        }
        return x;
    }
}
