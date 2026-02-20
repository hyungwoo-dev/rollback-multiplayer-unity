using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[ManagedStateIgnore]
public abstract partial class BaseWorldManager
{
    private static Debug Debug = new(nameof(BaseWorldManager));

    public BattleWorld LocalWorld { get; private set; }
    public BattleWorld FutureWorld { get; private set; }
    public BattleCamera Camera { get; private set; }

    protected int PlayerID { get; set; } = 0;

    public abstract int BattleTimeMillis { get; }
    public BaseWorldManager()
    {
        InitalizeInputManager();
        InitializePool();
        LocalWorld = WorldPool.Get();
        FutureWorld = WorldPool.Get();
    }

    public virtual void Setup()
    {
        var localWorldScene = new BattleWorldScene(this, BattleWorldSceneKind.VIEW, LayerMask.NameToLayer(BattleLayerMaskNames.LOCAL));
        localWorldScene.Load();
        LocalWorld.SetWorldScene(localWorldScene);

        var futureWorldScene = new BattleWorldScene(this, BattleWorldSceneKind.PLAYING, LayerMask.NameToLayer(BattleLayerMaskNames.FUTURE));
        futureWorldScene.Load();
        FutureWorld.SetWorldScene(futureWorldScene);
    }

    public virtual bool IsSetupCompleted()
    {
        return LocalWorld.IsSceneLoaded() && FutureWorld.IsSceneLoaded();
    }

    public virtual void OnSetupCompleted()
    {

    }

    public virtual void Initialize(BattleCamera camera)
    {
        Camera = camera;
        LocalWorld.Initialize();
        FutureWorld.Initialize();
    }

    public virtual void AdvanceFrame(in BattleFrame frame)
    {
        // 이 때, FutureWorld와 LocalWorld의 상태가 같아진다.
        LocalWorld.Apply(FutureWorld);

        ExecuteWorldEventInfos(FutureWorld.NextFrame, WorldEventInfos);

        if (WorldEventInfos.Count > 0)
        {
            
            WorldEventInfos.Clear();

            //FutureWorld.Release();
            //FutureWorld = WorldPool.Get();
            //FutureWorld.CopyFrom(LocalWorld);
        }
        
        FutureWorld.AdvanceFrame(frame);
    }

    protected virtual void ExecuteWorldEventInfos(int frame, List<BattleWorldEventInfo> worldEventInfos)
    {
        FutureWorld.ExecuteWorldEventInfos(worldEventInfos);
    }

    public virtual void OnUpdate(in BattleFrame frame)
    {
        InputManager.OnUpdate(InputContext);
        LocalWorld.Interpolate(frame, FutureWorld);
    }

    public virtual void Dispose()
    {
        DisposeInputManager();

        foreach (var worldEventInfo in WorldEventInfos)
        {
            worldEventInfo.Release(this);
        }
        WorldEventInfos.Clear();

        LocalWorld.Release();
        LocalWorld = null;

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
    private List<BattleWorldEventInfo> WorldEventInfos { get; set; } = new(16);

    private void InitalizeInputManager()
    {
        InputManager.OnInputMoveLeftArrowDown += OnPlayerInputMoveLeftArrowDown;
        InputManager.OnInputMoveLeftArrowUp += OnPlayerInputMoveLeftArrowUp;
        InputManager.OnInputMoveRightArrowDown += OnPlayerInputMoveRightArrowDown;
        InputManager.OnInputMoveRightArrowUp += OnPlayerInputMoveRightArrowUp;
        InputManager.OnInputAttack1 += OnPlayerInputAttack1;
        InputManager.OnInputAttack2 += OnPlayerInputAttack2;
        InputManager.OnInputFire += OnPlayerInputFire;
        InputManager.OnInputJump += OnPlayerInputJump;
    }

    private void OnPlayerInputMoveRightArrowDown()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.MOVE_RIGHT_ARROW_DOWN, PlayerID);
    }

    private void OnPlayerInputMoveRightArrowUp()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.MOVE_RIGHT_ARROW_UP, PlayerID);
    }

    private void OnPlayerInputMoveLeftArrowDown()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.MOVE_LEFT_ARROW_DOWN, PlayerID);
    }

    private void OnPlayerInputMoveLeftArrowUp()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.MOVE_LEFT_ARROW_UP, PlayerID);
    }

    private void OnPlayerInputAttack1()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.ATTACK1, PlayerID);
    }

    private void OnPlayerInputAttack2()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.ATTACK2, PlayerID);
    }

    private void OnPlayerInputFire()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.FIRE, PlayerID);
    }

    private void OnPlayerInputJump()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.JUMP, PlayerID);
    }

    protected virtual void PerformWorldEventInfo(BattleWorldInputEventType inputEventType, int unitId)
    {
        var eventInfo = WorldEventInfoPool.Get();
        eventInfo.WorldInputEventType = inputEventType;
        eventInfo.UnitID = PlayerID;
        eventInfo.TargetFrame = FutureWorld.NextFrame;
        eventInfo.BattleTimeMillis = BattleTimeMillis;
        WorldEventInfos.Add(eventInfo);
    }

    private void DisposeInputManager()
    {
        InputManager.OnInputMoveLeftArrowDown -= OnPlayerInputMoveLeftArrowDown;
        InputManager.OnInputMoveLeftArrowUp -= OnPlayerInputMoveLeftArrowUp;
        InputManager.OnInputMoveRightArrowDown -= OnPlayerInputMoveRightArrowDown;
        InputManager.OnInputMoveRightArrowUp -= OnPlayerInputMoveRightArrowUp;
        InputManager.OnInputAttack1 -= OnPlayerInputAttack2;
        InputManager.OnInputAttack2 -= OnPlayerInputAttack2;
        InputManager.OnInputFire -= OnPlayerInputFire;
        InputManager.OnInputJump -= OnPlayerInputJump;
        InputManager.Dispose();
    }

    #endregion Input

    #region Pool

    public ObjectPool<BattleWorld> WorldPool { get; private set; }
    public ObjectPool<BattleWorldEventInfo> WorldEventInfoPool { get; private set; }

    private void InitializePool()
    {
        WorldPool = new ObjectPool<BattleWorld>(createFunc: () => new BattleWorld(this), defaultCapacity: 2);
        WorldEventInfoPool = new ObjectPool<BattleWorldEventInfo>(createFunc: () => new BattleWorldEventInfo(this), defaultCapacity: 32);
    }

    private void DisposePool()
    {
        WorldPool.Dispose();
        WorldEventInfoPool.Dispose();
    }

    #endregion Pool
}
