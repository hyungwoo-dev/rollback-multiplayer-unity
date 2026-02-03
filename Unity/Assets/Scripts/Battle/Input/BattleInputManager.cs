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

    public event Action OnInputMoveBackDown = null;
    public event Action OnInputMoveBackUp = null;
    public event Action OnInputMoveForwardDown = null;
    public event Action OnInputMoveForwardUp = null;
    public event Action OnInputAttack1 = null;
    public event Action OnInputAttack2 = null;
    public event Action OnInputFire = null;
    public event Action OnInputJump = null;

    public void OnUpdate(float time, BattleInputContext context)
    {
        UpdateInputEvents(time, context);
    }

    private void UpdateInputEvents(float time, BattleInputContext context)
    {
        if (Input.GetKeyDown(context.MoveBackKeyCode))
        {
            OnInputMoveBackDown?.Invoke();
        }

        if (Input.GetKeyUp(context.MoveBackKeyCode))
        {
            OnInputMoveBackUp?.Invoke();
        }

        if (Input.GetKeyDown(context.MoveForwardKeyCode))
        {
            OnInputMoveForwardDown?.Invoke();
        }

        if (Input.GetKeyUp(context.MoveForwardKeyCode))
        {
            OnInputMoveForwardUp?.Invoke();
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
        OnInputMoveBackDown = null;
        OnInputMoveBackUp = null;
        OnInputAttack1 = null;
        OnInputAttack2 = null;
        OnInputFire = null;
        OnInputJump = null;
    }
}
