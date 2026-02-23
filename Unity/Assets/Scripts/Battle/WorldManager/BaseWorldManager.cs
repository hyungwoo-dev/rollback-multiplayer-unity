using System.Collections.Generic;
using UnityEngine;

[ManagedStateIgnore]
public abstract partial class BaseWorldManager
{
    private static Debug Debug = new(nameof(BaseWorldManager));

    public BattleWorldScene LocalWorldScene { get; private set; }
    public BattleWorld FutureWorld { get; protected set; }
    public BattleCamera Camera { get; private set; }

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

    public virtual void Initialize(BattleCamera camera, in BattleFrame frame)
    {
        Camera = camera;
        LocalWorldScene.Initialize();
        FutureWorld.Initialize();
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
        InputManager.OnUpdate(InputContext);
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

    private BattleInputManager InputManager { get; } = new BattleInputManager();
    private BattleInputContext InputContext { get; } = new BattleInputContext();

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
        FutureWorld.PerformWorldEventInfo(BattleWorldInputEventType.MOVE_RIGHT_ARROW_DOWN, PlayerID, BattleTimeMillis);
    }

    private void OnPlayerInputMoveRightArrowUp()
    {
        FutureWorld.PerformWorldEventInfo(BattleWorldInputEventType.MOVE_RIGHT_ARROW_UP, PlayerID, BattleTimeMillis);
    }

    private void OnPlayerInputMoveLeftArrowDown()
    {
        FutureWorld.PerformWorldEventInfo(BattleWorldInputEventType.MOVE_LEFT_ARROW_DOWN, PlayerID, BattleTimeMillis);
    }

    private void OnPlayerInputMoveLeftArrowUp()
    {
        FutureWorld.PerformWorldEventInfo(BattleWorldInputEventType.MOVE_LEFT_ARROW_UP, PlayerID, BattleTimeMillis);
    }

    private void OnPlayerInputAttack1()
    {
        FutureWorld.PerformWorldEventInfo(BattleWorldInputEventType.ATTACK1, PlayerID, BattleTimeMillis);
    }

    private void OnPlayerInputAttack2()
    {
        FutureWorld.PerformWorldEventInfo(BattleWorldInputEventType.ATTACK2, PlayerID, BattleTimeMillis);
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
