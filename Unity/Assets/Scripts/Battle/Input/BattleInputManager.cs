using System;
using UnityEngine;

public class BattleInputManager
{
    public event Action OnInputMoveLeftArrowDown = null;
    public event Action OnInputMoveLeftArrowUp = null;
    public event Action OnInputMoveRightArrowDown = null;
    public event Action OnInputMoveRightArrowUp = null;
    public event Action OnInputAttack1 = null;
    public event Action OnInputAttack2 = null;

    public void OnUpdate(BattleInputContext context)
    {
        UpdateInputEvents(context);
    }

    private void UpdateInputEvents(BattleInputContext context)
    {
        if (Input.GetKeyDown(context.MoveLeftArrowKeyCode))
        {
            OnInputMoveLeftArrowDown?.Invoke();
        }

        if (Input.GetKeyUp(context.MoveLeftArrowKeyCode))
        {
            OnInputMoveLeftArrowUp?.Invoke();
        }

        if (Input.GetKeyDown(context.MoveRightArrowKeyCode))
        {
            OnInputMoveRightArrowDown?.Invoke();
        }

        if (Input.GetKeyUp(context.MoveRightArrowKeyCode))
        {
            OnInputMoveRightArrowUp?.Invoke();
        }

        if (Input.GetKeyDown(context.Attack1KeyCode))
        {
            OnInputAttack1?.Invoke();
        }

        if (Input.GetKeyDown(context.Attack2KeyCode))
        {
            OnInputAttack2?.Invoke();
        }
    }

    public void Dispose()
    {
        OnInputMoveLeftArrowDown = null;
        OnInputMoveLeftArrowUp = null;
        OnInputAttack1 = null;
        OnInputAttack2 = null;
    }
}
