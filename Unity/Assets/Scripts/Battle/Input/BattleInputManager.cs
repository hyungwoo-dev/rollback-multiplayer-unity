using System;

public class BattleInputManager
{
    public event Action<BattleWorldInputEventType> OnFrameEventImmediately;

    public void Initialize()
    {
        NativeBackgroundRawInput.OnKeyDown += HandleOnKeyDown;
        NativeBackgroundRawInput.OnKeyUp += HandleOnKeyUp;
    }

    private void HandleOnKeyDown(RawKey obj)
    {
        var frameEventType = GetFrameEventType(obj, KeyEventType.Down);
        if (frameEventType != BattleWorldInputEventType.NONE)
        {
            OnFrameEventImmediately?.Invoke(frameEventType);
        }
    }

    private void HandleOnKeyUp(RawKey obj)
    {
        var frameEventType = GetFrameEventType(obj, KeyEventType.Up);
        if (frameEventType != BattleWorldInputEventType.NONE)
        {
            OnFrameEventImmediately?.Invoke(frameEventType);
        }
    }

    public void Dispose()
    {
        NativeBackgroundRawInput.OnKeyDown -= HandleOnKeyDown;
        NativeBackgroundRawInput.OnKeyUp -= HandleOnKeyUp;
    }

    private static BattleWorldInputEventType GetFrameEventType(RawKey key, KeyEventType eventType)
    {
        switch (eventType)
        {
            case KeyEventType.Down:
            {
                switch (key)
                {
                    case RawKey.A:
                    {
                        return BattleWorldInputEventType.ATTACK1;
                    }
                    case RawKey.S:
                    {
                        return BattleWorldInputEventType.ATTACK2;
                    }
                    case RawKey.LeftArrow:
                    {
                        return BattleWorldInputEventType.MOVE_LEFT_ARROW_DOWN;
                    }
                    case RawKey.RightArrow:
                    {
                        return BattleWorldInputEventType.MOVE_RIGHT_ARROW_DOWN;
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
                        return BattleWorldInputEventType.MOVE_LEFT_ARROW_UP;
                    }
                    case RawKey.RightArrow:
                    {
                        return BattleWorldInputEventType.MOVE_RIGHT_ARROW_UP;
                    }
                }
                break;
            }
        }

        return BattleWorldInputEventType.NONE;
    }
}
