using System;

public class BattleWindowsInputManager : IBattleInputManager
{
    private object _lock = new();
    public event Action OnInputMoveLeftArrowDown;
    public event Action OnInputMoveLeftArrowUp;
    public event Action OnInputMoveRightArrowDown;
    public event Action OnInputMoveRightArrowUp;
    public event Action OnInputAttack1;
    public event Action OnInputAttack2;


    public event Action<FrameEventType> OnFrameEventImmediately;

    public void Initialize()
    {
        NativeBackgroundRawInput.OnKeyDown += HandleOnKeyDown;
        NativeBackgroundRawInput.OnKeyUp += HandleOnKeyUp;
    }

    public void OnUpdate()
    {
        //using var _ = ListPool<FrameEventType>.Get(out var tempList);
        //lock (_lock)
        //{
        //    tempList.AddRange(_inputKeyEvents);
        //    _inputKeyEvents.Clear();
        //}

        //foreach (var frameEventType in tempList)
        //{
        //    switch (frameEventType)
        //    {
        //        case FrameEventType.LEFT_ARROW_DOWN:
        //        {
        //            OnInputMoveLeftArrowDown?.Invoke();
        //            break;
        //        }
        //        case FrameEventType.LEFT_ARROW_UP:
        //        {
        //            OnInputMoveLeftArrowUp?.Invoke();
        //            break;
        //        }
        //        case FrameEventType.RIGHT_ARROW_DOWN:
        //        {
        //            OnInputMoveRightArrowDown?.Invoke();
        //            break;
        //        }
        //        case FrameEventType.RIGHT_ARROW_UP:
        //        {
        //            OnInputMoveRightArrowUp?.Invoke();
        //            break;
        //        }
        //        case FrameEventType.ATTACK1:
        //        {
        //            OnInputAttack1?.Invoke();
        //            break;
        //        }
        //        case FrameEventType.ATTACK2:
        //        {
        //            OnInputAttack2?.Invoke();
        //            break;
        //        }
        //    }
        //}
    }

    private void HandleOnKeyDown(RawKey obj)
    {
        var frameEventType = GetFrameEventType(obj, KeyEventType.Down);
        if (frameEventType != FrameEventType.NONE)
        {
            OnFrameEventImmediately?.Invoke(frameEventType);
        }
    }

    private void HandleOnKeyUp(RawKey obj)
    {
        var frameEventType = GetFrameEventType(obj, KeyEventType.Up);
        if (frameEventType != FrameEventType.NONE)
        {
            OnFrameEventImmediately?.Invoke(frameEventType);
        }
    }

    public void Dispose()
    {
        NativeBackgroundRawInput.OnKeyDown -= HandleOnKeyDown;
        NativeBackgroundRawInput.OnKeyUp -= HandleOnKeyUp;
    }

    private static FrameEventType GetFrameEventType(RawKey key, KeyEventType eventType)
    {
        switch (eventType)
        {
            case KeyEventType.Down:
            {
                switch (key)
                {
                    case RawKey.A:
                    {
                        return FrameEventType.ATTACK1;
                    }
                    case RawKey.S:
                    {
                        return FrameEventType.ATTACK2;
                    }
                    case RawKey.LeftArrow:
                    {
                        return FrameEventType.LEFT_ARROW_DOWN;
                    }
                    case RawKey.RightArrow:
                    {
                        return FrameEventType.RIGHT_ARROW_DOWN;
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
                        return FrameEventType.LEFT_ARROW_UP;
                    }
                    case RawKey.RightArrow:
                    {
                        return FrameEventType.RIGHT_ARROW_UP;
                    }
                }
                break;
            }
        }

        return FrameEventType.NONE;
    }
}
