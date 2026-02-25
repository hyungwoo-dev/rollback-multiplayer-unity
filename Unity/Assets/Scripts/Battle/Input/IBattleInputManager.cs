using System;

public interface IBattleInputManager
{
    event Action OnInputMoveLeftArrowDown;
    event Action OnInputMoveLeftArrowUp;
    event Action OnInputMoveRightArrowDown;
    event Action OnInputMoveRightArrowUp;
    event Action OnInputAttack1;
    event Action OnInputAttack2;

    void Initialize();
    void OnUpdate();
    void Dispose();
}
