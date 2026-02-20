using System;

public class TimeUtils
{
    public static long UtcNowUnixTimeMillis => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}