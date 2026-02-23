using System;
using System.Collections.Generic;

public class Pool<T>
{
    private Func<T> _createFunc;
    private Action<T> _onReleaseAction;
    private Stack<T> _stack;

    public Pool(Func<T> createFunc, Action<T> onRelease = null)
    {
        _stack = new Stack<T>();
        _createFunc = createFunc;
        _onReleaseAction = onRelease;
    }

    public T Get()
    {
        if (_stack.TryPop(out var result))
        {
            return result;
        }
        return _createFunc();

    }

    public void Release(T instance)
    {
        _onReleaseAction?.Invoke(instance);
        _stack.Push(instance);
    }

    public void Dispose()
    {
        _stack.Clear();
        _stack = null;
        _createFunc = null;
        _onReleaseAction = null;
    }
}