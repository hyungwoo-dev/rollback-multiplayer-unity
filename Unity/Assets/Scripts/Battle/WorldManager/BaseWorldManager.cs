using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using InputManager = BattleInputManager;
#elif UNITY_STANDALONE_WIN
using InputManager = BattleWindowsInputManager;
#endif

[ManagedStateIgnore]
public abstract partial class BaseWorldManager
{
    private static Debug Debug = new(nameof(BaseWorldManager));

    public BattleWorldScene LocalWorldScene { get; private set; }
    public BattleWorld FutureWorld { get; protected set; }
    public int PlayerID { get; protected set; } = 0;

    public abstract int BattleTimeMillis { get; }


    public BaseWorldManager()
    {
        InitalizeInputManager();
        InitializePool();
        FutureWorld = WorldPool.Get();
    }

    public virtual void Setup()
    {
        var localWorldScene = new BattleWorldScene(this, LayerMask.NameToLayer(BattleLayerMaskNames.LOCAL));
        localWorldScene.Load();
        LocalWorldScene = localWorldScene;

        FutureWorld.SetWorldScene(localWorldScene);
    }

    public virtual bool IsSetupCompleted()
    {
        return LocalWorldScene.IsSceneLoaded();
    }

    public virtual void OnSetupCompleted()
    {

    }

    public virtual void Initialize(in BattleFrame frame)
    {
        LocalWorldScene.Initialize();
        FutureWorld.Initialize();
        InputManager.Initialize();
    }

    public virtual void AdvanceFrame(in BattleFrame frame)
    {
        FutureWorld.ApplyTo(LocalWorldScene);

        var executeWorldEventInfos = FutureWorld.WorldEventInfoListPool.Get();
        FutureWorld.ApplyWorldEventInfos(executeWorldEventInfos);
        OnExecuteWorldEventInfos(FutureWorld.NextFrame, executeWorldEventInfos);
        FutureWorld.WorldEventInfoListPool.Release(executeWorldEventInfos);

        FutureWorld.AdvanceFrame(frame);
    }

    protected virtual void OnExecuteWorldEventInfos(int frame, List<BattleWorldEventInfo> worldEventInfos)
    {

    }

    public virtual void OnUpdate(in BattleFrame frame)
    {
        InputManager.OnUpdate();
        FutureWorld.Interpolate(frame, LocalWorldScene);
    }

    public virtual void Dispose()
    {
        DisposeInputManager();

        LocalWorldScene.Dispose();
        LocalWorldScene = null;

        FutureWorld.Release();
        FutureWorld = null;

        DisposePool();
    }

    public virtual bool IsStarted()
    {
        return true;
    }

    #region Input

    protected IBattleInputManager InputManager { get; } = new InputManager();

    private void InitalizeInputManager()
    {
        InputManager.OnInputMoveLeftArrowDown += OnPlayerInputMoveLeftArrowDown;
        InputManager.OnInputMoveLeftArrowUp += OnPlayerInputMoveLeftArrowUp;
        InputManager.OnInputMoveRightArrowDown += OnPlayerInputMoveRightArrowDown;
        InputManager.OnInputMoveRightArrowUp += OnPlayerInputMoveRightArrowUp;
        InputManager.OnInputAttack1 += OnPlayerInputAttack1;
        InputManager.OnInputAttack2 += OnPlayerInputAttack2;
    }

    private void OnPlayerInputMoveRightArrowDown()
    {
        FutureWorld.AddWorldEventInfo(BattleWorldInputEventType.MOVE_RIGHT_ARROW_DOWN, PlayerID, BattleTimeMillis);
    }

    private void OnPlayerInputMoveRightArrowUp()
    {
        FutureWorld.AddWorldEventInfo(BattleWorldInputEventType.MOVE_RIGHT_ARROW_UP, PlayerID, BattleTimeMillis);
    }

    private void OnPlayerInputMoveLeftArrowDown()
    {
        FutureWorld.AddWorldEventInfo(BattleWorldInputEventType.MOVE_LEFT_ARROW_DOWN, PlayerID, BattleTimeMillis);
    }

    private void OnPlayerInputMoveLeftArrowUp()
    {
        FutureWorld.AddWorldEventInfo(BattleWorldInputEventType.MOVE_LEFT_ARROW_UP, PlayerID, BattleTimeMillis);
    }

    private void OnPlayerInputAttack1()
    {
        FutureWorld.AddWorldEventInfo(BattleWorldInputEventType.ATTACK1, PlayerID, BattleTimeMillis);
    }

    private void OnPlayerInputAttack2()
    {
        FutureWorld.AddWorldEventInfo(BattleWorldInputEventType.ATTACK2, PlayerID, BattleTimeMillis);
    }

    private void DisposeInputManager()
    {
        InputManager.OnInputMoveLeftArrowDown -= OnPlayerInputMoveLeftArrowDown;
        InputManager.OnInputMoveLeftArrowUp -= OnPlayerInputMoveLeftArrowUp;
        InputManager.OnInputMoveRightArrowDown -= OnPlayerInputMoveRightArrowDown;
        InputManager.OnInputMoveRightArrowUp -= OnPlayerInputMoveRightArrowUp;
        InputManager.OnInputAttack1 -= OnPlayerInputAttack2;
        InputManager.OnInputAttack2 -= OnPlayerInputAttack2;
        InputManager.Dispose();
    }

    #endregion Input

    #region Pool

    public Pool<BattleWorld> WorldPool { get; private set; }

    private void InitializePool()
    {
        WorldPool = new Pool<BattleWorld>(createFunc: () => new BattleWorld(this));
    }

    private void DisposePool()
    {
        WorldPool.Dispose();
    }

    #endregion Pool
}
