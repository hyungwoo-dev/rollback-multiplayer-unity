using System.Collections.Generic;
using UnityEngine;

[ManagedStateIgnore]
public abstract partial class BaseWorldManager
{
    private static readonly int DefaultHash = int.MaxValue;

    private static Debug Debug = new(nameof(BaseWorldManager));

    public BattleWorldScene LocalWorldScene { get; private set; }
    public BattleWorld FutureWorld { get; protected set; }
    public BattleCamera Camera { get; private set; }

    protected int PlayerID { get; set; } = 0;

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

    public virtual void Initialize(BattleCamera camera)
    {
        Camera = camera;
        LocalWorldScene.Initialize();
        FutureWorld.Initialize();
    }

    public virtual void AdvanceFrame(in BattleFrame frame)
    {
        // 이 때, FutureWorld와 LocalWorld의 상태가 같아진다.
        FutureWorld.ApplyTo(LocalWorldScene);

        if (WorldEventInfos.Count == 0)
        {
            PerformWorldEventInfo(BattleWorldInputEventType.NONE, PlayerID);
        }

        ExecuteWorldEventInfos(FutureWorld.NextFrame, WorldEventInfos);

        if (WorldEventInfos.Count > 0)
        {
            WorldEventInfos.Clear();
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
        FutureWorld.Interpolate(frame, LocalWorldScene);
    }

    public virtual void Dispose()
    {
        DisposeInputManager();

        foreach (var worldEventInfo in WorldEventInfos)
        {
            worldEventInfo.Release(this);
        }
        WorldEventInfos.Clear();

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

    protected int GetWorldEventInfosHash(List<BattleWorldEventInfo> worldEventInfos)
    {
        var hash = DefaultHash;
        foreach (var worldEventInfo in worldEventInfos)
        {
            // None 타입 커맨드는 아무런 입력을 받지 못했다는 것을 의미하기 때문에, 월드에 영향을 주지 않는다.
            if (worldEventInfo.WorldInputEventType == BattleWorldInputEventType.NONE)
            {
                continue;
            }

            hash ^= worldEventInfo.GetHashCode();
        }
        return hash;
    }


    protected void ReleaseWorldEventInfos(List<BattleWorldEventInfo> worldEventInfos)
    {
        foreach (var worldEventInfo in worldEventInfos)
        {
            worldEventInfo.Release(this);
        }

        ConcurrentListPool<BattleWorldEventInfo>.Release(worldEventInfos);
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
        InputManager.Dispose();
    }

    #endregion Input

    #region Pool

    public ConcurrentPool<BattleWorld> WorldPool { get; private set; }
    public ConcurrentPool<BattleWorldEventInfo> WorldEventInfoPool { get; private set; }

    private void InitializePool()
    {
        WorldPool = new ConcurrentPool<BattleWorld>(createFunc: () => new BattleWorld(this));
        WorldEventInfoPool = new ConcurrentPool<BattleWorldEventInfo>(createFunc: () => new BattleWorldEventInfo(this));
    }

    private void DisposePool()
    {
        WorldPool.Dispose();
        WorldEventInfoPool.Dispose();
    }

    #endregion Pool
}
