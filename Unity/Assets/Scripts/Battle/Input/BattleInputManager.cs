using System;
using UnityEngine;

public class BattleInputManager
{
    public event Action OnInputMoveBackDown = null;
    public event Action OnInputMoveBackUp = null;
    public event Action OnInputMoveForwardDown = null;
    public event Action OnInputMoveForwardUp = null;
    public event Action OnInputAttack1 = null;
    public event Action OnInputAttack2 = null;
    public event Action OnInputFire = null;
    public event Action OnInputJump = null;

    public void OnUpdate(BattleInputContext context)
    {
        UpdateInputEvents(context);
    }

    private void UpdateInputEvents(BattleInputContext context)
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
