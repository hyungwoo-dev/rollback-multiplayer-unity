using System;
using UnityEngine;

public class BattleInputManager
{
    private class DashInputInfo
    {
        private const float DASH_INPUT_DETECT_TIME_SEC = 0.3f;
        private const int DASH_INVOKE_INPUT_COUNT = 2;

        private int _dashInputCounter = 0;
        private float _lastestInputTime = 0.0f;

        public void OnUpdate(float time)
        {
            if (time - _lastestInputTime > DASH_INPUT_DETECT_TIME_SEC)
            {
                _dashInputCounter = 0;
            }
        }

        public bool OnInput(float time)
        {
            _lastestInputTime = time;
            var result = ++_dashInputCounter == DASH_INVOKE_INPUT_COUNT;
            if (result)
            {
                _dashInputCounter = 0;
            }
            return result;
        }
    }

    public event Action OnInputLeftDash = null;
    public event Action OnInputRightDash = null;
    public event Action OnInputAttack1 = null;
    public event Action OnInputAttack2 = null;
    public event Action OnInputFire = null;
    public event Action OnInputJump = null;

    private DashInputInfo _leftDashInputInfo = new();
    private DashInputInfo _rightDashInputInfo = new();

    public void OnUpdate(float time, BattleInputContext context)
    {
        UpdateInputEvents(time, context);
        UpdateDashInfo(time);
    }

    private void UpdateDashInfo(float time)
    {
        _leftDashInputInfo.OnUpdate(time);
        _rightDashInputInfo.OnUpdate(time);
    }

    private void UpdateInputEvents(float time, BattleInputContext context)
    {
        if (Input.GetKeyDown(context.LeftDashKeyCode))
        {
            if (_leftDashInputInfo.OnInput(time))
            {
                OnInputLeftDash?.Invoke();
            }
        }

        if (Input.GetKeyDown(context.RightDashKeyCode))
        {
            if (_rightDashInputInfo.OnInput(time))
            {
                OnInputRightDash?.Invoke();
            }
        }

        if (Input.GetKeyDown(context.Attack1KeyCode))
        {
            OnInputAttack1?.Invoke();
        }

        if (Input.GetKeyDown(context.Attack2KeyCode))
        {
            OnInputAttack2?.Invoke();
        }

        if (Input.GetKeyDown(context.FireKeyCode))
        {
            OnInputFire?.Invoke();
        }

        if (Input.GetKeyDown(context.JumpKeyCode))
        {
            OnInputJump?.Invoke();
        }
    }

    public void Dispose()
    {
        OnInputLeftDash = null;
        OnInputRightDash = null;
        OnInputAttack1 = null;
        OnInputAttack2 = null;
        OnInputFire = null;
        OnInputJump = null;
    }
}
