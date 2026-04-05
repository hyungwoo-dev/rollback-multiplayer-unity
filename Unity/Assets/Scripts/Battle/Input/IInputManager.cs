using System;

public interface IInputManager
{
    event Action<BattleWorldInputEventType> OnFrameEventImmediately;
    void Initialize();
    void Dispose();
}