using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;

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

    protected Dictionary<int, List<BattleWorldEventInfo>> LocalWorldEventInfos { get; private set; } = new();

    public BaseWorldManager()
    {
        InitalizeInputManager();
        InitializePool();
        FutureWorld = WorldPool.Get();
    }

    public virtual void Setup()
    {
        var localWorldScene = new BattleWorldScene(this, BattleWorldSceneKind.VIEW, LayerMask.NameToLayer(BattleLayerMaskNames.LOCAL));
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
        var newWorldEventInfos = ListPool<BattleWorldEventInfo>.Get();
        newWorldEventInfos.AddRange(worldEventInfos);
        LocalWorldEventInfos.Add(frame, newWorldEventInfos);

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

        foreach (var worldEventInfos in LocalWorldEventInfos.Values)
        {
            ReleaseWorldEventInfos(worldEventInfos);
        }
        LocalWorldEventInfos.Clear();

        DisposePool();
    }

    public virtual bool IsStarted()
    {
        return true;
    }

    public IEnumerator CoSelfResimulate(BattleFrame frame)
    {
        var world = WorldPool.Get();
        var worldScene = new BattleWorldScene(this, BattleWorldSceneKind.PLAYING, LayerMask.NameToLayer(BattleLayerMaskNames.FUTURE));
        worldScene.Load();
        world.SetWorldScene(worldScene);

        while (!worldScene.IsSceneLoaded())
        {
            yield return null;
        }

        world.Initialize();

        var info = FutureWorld.GetInfo();

        var clone = new Dictionary<int, List<BattleWorldEventInfo>>(LocalWorldEventInfos);
        foreach (var key in clone.Keys.ToList())
        {
            if (clone[key].All(a => a.WorldInputEventType == BattleWorldInputEventType.NONE))
            {
                clone.Remove(key);
            }
        }

        var futureNextFrame = FutureWorld.NextFrame;
        var targetFrame = futureNextFrame - Mathf.RoundToInt(1.0f / Time.fixedDeltaTime);
        while (world.NextFrame < targetFrame)
        {
            if (clone.TryGetValue(world.NextFrame, out var temp))
            {
                world.ExecuteWorldEventInfos(temp);
            }

            world.AdvanceFrame(frame);
        }

        FutureWorld.Release();
        FutureWorld = WorldPool.Get();
        FutureWorld.CopyFrom(world);

        while (FutureWorld.NextFrame < futureNextFrame)
        {
            if (clone.TryGetValue(FutureWorld.NextFrame, out var temp))
            {
                FutureWorld.ExecuteWorldEventInfos(temp);
            }
            FutureWorld.AdvanceFrame(frame);
        }

        var newInfo = FutureWorld.GetInfo();

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("===OriginInfos===");
        stringBuilder.AppendLine(info);
        stringBuilder.AppendLine("===Resimulate===");
        stringBuilder.AppendLine(newInfo);
        stringBuilder.AppendLine("=================");
        Debug.Log(stringBuilder.ToString());

        world.Dispose();
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

        ListPool<BattleWorldEventInfo>.Release(worldEventInfos);
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
        WorldEventInfoPool = new ObjectPool<BattleWorldEventInfo>(createFunc: () => new BattleWorldEventInfo(this), defaultCapacity: 128);
    }

    private void DisposePool()
    {
        WorldPool.Dispose();
        WorldEventInfoPool.Dispose();
    }

    #endregion Pool
}
