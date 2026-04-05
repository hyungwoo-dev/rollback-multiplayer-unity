using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class BattleUnityInputManager : IInputManager
{
    private BattleUnityInputUpdater _inputUpdater;
    public event Action<BattleWorldInputEventType> OnFrameEventImmediately;

    public void Initialize()
    {
        if (_inputUpdater == null)
        {
            _inputUpdater = BattleUnityInputUpdater.Create();
            _inputUpdater.OnUpdate += OnUpdate;
        }
    }

    public void Dispose()
    {
        if (_inputUpdater != null)
        {
            _inputUpdater.OnUpdate -= OnUpdate;
            Object.Destroy(_inputUpdater);
            _inputUpdater = null;
        }
    }

    private void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            OnFrameEventImmediately?.Invoke(BattleWorldInputEventType.MOVE_LEFT_ARROW_DOWN);
        }
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            OnFrameEventImmediately?.Invoke(BattleWorldInputEventType.MOVE_LEFT_ARROW_UP);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnFrameEventImmediately?.Invoke(BattleWorldInputEventType.MOVE_RIGHT_ARROW_DOWN);
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            OnFrameEventImmediately?.Invoke(BattleWorldInputEventType.MOVE_RIGHT_ARROW_UP);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            OnFrameEventImmediately?.Invoke(BattleWorldInputEventType.ATTACK1);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            OnFrameEventImmediately?.Invoke(BattleWorldInputEventType.ATTACK2);
        }
    }
}
