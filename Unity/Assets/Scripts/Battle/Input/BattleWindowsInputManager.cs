using System;
using System.Collections.Generic;
using UnityEngine.Pool;

public class BattleWindowsInputManager : IBattleInputManager
{
    private object _lock = new();
    public event Action OnInputMoveLeftArrowDown;
    public event Action OnInputMoveLeftArrowUp;
    public event Action OnInputMoveRightArrowDown;
    public event Action OnInputMoveRightArrowUp;
    public event Action OnInputAttack1;
    public event Action OnInputAttack2;

    private List<(RawKey Key, KeyEventType EventType)> _inputKeyEvents = new();

    public void Initialize()
    {
        NativeBackgroundRawInput.OnKeyDown += HandleOnKeyDown;
        NativeBackgroundRawInput.OnKeyUp += HandleOnKeyUp;
        NativeBackgroundRawInput.Initialize();
    }

    public void OnUpdate()
    {
        using var _ = ListPool<(RawKey Key, KeyEventType EventType)>.Get(out var tempList);
        lock (_lock)
        {
            tempList.AddRange(_inputKeyEvents);
            _inputKeyEvents.Clear();
        }

        foreach (var (key, eventType) in tempList)
        {
            InvokeEvent(key, eventType);
        }
    }

    private void InvokeEvent(RawKey key, KeyEventType eventType)
    {
        switch (eventType)
        {
            case KeyEventType.Down:
            {
                switch (key)
                {
                    case RawKey.A:
                    {
                        OnInputAttack1?.Invoke();
                        break;
                    }
                    case RawKey.S:
                    {
                        OnInputAttack2?.Invoke();
                        break;
                    }
                    case RawKey.LeftArrow:
                    {
                        OnInputMoveLeftArrowDown?.Invoke();
                        break;
                    }
                    case RawKey.RightArrow:
                    {
                        OnInputMoveRightArrowDown?.Invoke();
                        break;
                    }
                }
                break;
            }
            case KeyEventType.Up:
            {
                switch (key)
                {
                    case RawKey.LeftArrow:
                    {
                        OnInputMoveLeftArrowUp?.Invoke();
                        break;
                    }
                    case RawKey.RightArrow:
                    {
                        OnInputMoveRightArrowUp?.Invoke();
                        break;
                    }
                }
                break;
            }
        }
    }

    private void HandleOnKeyDown(RawKey obj)
    {
        lock (_lock)
        {
            _inputKeyEvents.Add((obj, KeyEventType.Down));
        }

    }

    private void HandleOnKeyUp(RawKey obj)
    {
        lock (_lock)
        {
            _inputKeyEvents.Add((obj, KeyEventType.Up));
        }
    }

    public void Dispose()
    {
        NativeBackgroundRawInput.Shutdown();
        NativeBackgroundRawInput.OnKeyDown -= HandleOnKeyDown;
        NativeBackgroundRawInput.OnKeyUp -= HandleOnKeyUp;
    }
}
