using System;
using System.Collections.Concurrent;

public class ConcurrentPool<T>
{
    private Func<T> _createFunc;
    private ConcurrentBag<T> _bag;

    public ConcurrentPool(Func<T> createFunc)
    {
        _bag = new ConcurrentBag<T>();
        _createFunc = createFunc;
    }

    public T Get()
    {
        return _bag.TryTake(out var result) ? result : _createFunc();
    }

    public void Release(T instance)
    {
        _bag.Add(instance);
    }

    public void Dispose()
    {
        _bag.Clear();
        _bag = null;
        _createFunc = null;
    }
}