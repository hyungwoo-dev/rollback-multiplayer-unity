using System.Collections.Generic;

public class ConcurrentListPool<T>
{
    private static ConcurrentPool<List<T>> _instance = new ConcurrentPool<List<T>>(() => new List<T>());

    public static List<T> Get()
    {
        return _instance.Get();
    }

    public static void Release(List<T> instance)
    {
        _instance.Release(instance);
    }
}