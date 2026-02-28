using System.Collections.Generic;
using UnityEngine;

[ManagedStateIgnore]
public abstract partial class BaseWorldManager
{
    private static Debug Debug = new(nameof(BaseWorldManager));

    private object _updateLock = new();
    public BattleWorldScene LocalWorldScene { get; private set; }
    public BattleWorld FutureWorld { get; protected set; }
    public int PlayerID { get; protected set; } = 0;

    public abstract int BattleTimeMillis { get; }
    protected Dictionary<int, List<BattleWorldEventInfo>> LocalWorldEventInfos { get; private set; } = new();

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
        lock (_updateLock)
        {
            FutureWorld.ApplyTo(LocalWorldScene);
            FutureWorld.AdvanceFrame(frame);
        }
    }

    protected virtual void HandleOnFrameEventImmediately(BattleWorldInputEventType worldInputEventType)
    {
        lock (_updateLock)
        {
            var worldEventInfo = CreateIntermidiateWorldEventInfo(worldInputEventType, FutureWorld.NextFrame, PlayerID, BattleTimeMillis);
            FutureWorld.ExecuteWorldEventInfo(worldEventInfo);

            if (LocalWorldEventInfos.TryGetValue(worldEventInfo.TargetFrame, out var list))
            {
                list.Add(worldEventInfo);
            }
            else
            {
                list = FutureWorld.WorldEventInfoListPool.Get();
                list.Add(worldEventInfo);
                LocalWorldEventInfos.Add(worldEventInfo.TargetFrame, list);
            }
        }
    }

    protected BattleWorldEventInfo CreateIntermidiateWorldEventInfo(BattleWorldInputEventType worldEventType, int frame, int userIndex, int battleTimeMillis)
    {
        var worldEventInfo = FutureWorld.WorldEventInfoPool.Get();
        worldEventInfo.TargetFrame = frame;
        worldEventInfo.WorldInputEventType = worldEventType;
        worldEventInfo.UnitID = userIndex;
        worldEventInfo.BattleTimeMillis = battleTimeMillis;
        return worldEventInfo;
    }


    public virtual void OnUpdate(in BattleFrame frame)
    {
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

    public virtual void OnStart()
    {

    }

    #region Input

    protected BattleInputManager InputManager { get; } = new BattleInputManager();

    private void InitalizeInputManager()
    {
        InputManager.OnFrameEventImmediately += HandleOnFrameEventImmediately;
        InputManager.Initialize();
    }

    private void DisposeInputManager()
    {
        InputManager.OnFrameEventImmediately -= HandleOnFrameEventImmediately;
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
