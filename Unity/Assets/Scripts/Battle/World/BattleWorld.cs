using System.Collections.Generic;
using UnityEngine.Pool;

[ManagedState]
public partial class BattleWorld
{
    private Debug Debug = new(nameof(BattleWorld));

    public int ID { get; set; }
    public BattleWorldManager WorldManager { get; }
    public BattleWorldScene WorldScene { get; private set; }
    public int CurrentFrame { get; private set; }
    public int NextFrame => CurrentFrame + 1;
    private List<BattleWorldEventInfo> WorldEventInfos { get; set; } = new();

    public List<BattleUnit> Units = new();

    public BattleWorld(BattleWorldManager worldManager)
    {
        InitializePool();
        WorldManager = worldManager;
        Units.Add(new BattleUnit(this));
        Units.Add(new BattleUnit(this));
    }

    public void Prepare(BattleWorldScene worldScene)
    {
        WorldScene = worldScene;
    }

    public bool IsReady()
    {
        return WorldScene.IsReady();
    }

    public void Initialize()
    {
        WorldScene.Initialize();
        foreach (var unit in Units)
        {
            unit.Initialize();
        }
    }

    public void OnUpdate(in BattleFrame frame)
    {
        foreach (var unit in Units)
        {
            unit.OnUpdate(frame);
        }
    }

    public void OnFixedUpdate(in BattleFrame frame)
    {
        CurrentFrame += 1;
        foreach (var unit in Units)
        {
            unit.OnFixedUpdate(frame);
        }

        ExecuteWorldEventInfos(CurrentFrame);

        WorldScene.SimulatePhysics(frame.DeltaTime);
    }

    public void AddWorldEventInfo(BattleWorldEventInfo worldEventInfo)
    {
        WorldEventInfos.Add(worldEventInfo);
    }

    private void ExecuteWorldEventInfos(int targetFrame)
    {
        var popEventCount = 0;
        for (int i = 0; i < WorldEventInfos.Count; i++)
        {
            var eventInfo = WorldEventInfos[i];
            if (eventInfo.TargetFrame <= targetFrame)
            {
                popEventCount += 1;
                ExecuteWorldEvent(eventInfo);
            }
            else
            {
                break;
            }
        }

        WorldEventInfos.RemoveRange(0, popEventCount);
    }

    private void ExecuteWorldEvent(BattleWorldEventInfo eventInfo)
    {
        var unit = GetUnit(eventInfo.UnitID);
        switch (eventInfo.WorldInputEventType)
        {
            case BattleWorldInputEventType.LEFT_DASH:
            {
                if (unit.CanDash())
                {
                    unit.DoLeftDash();
                }
                break;
            }
            case BattleWorldInputEventType.RIGHT_DASH:
            {
                if (unit.CanDash())
                {
                    unit.DoRightDash();
                }
                break;
            }
            case BattleWorldInputEventType.ATTACK1:
            {
                if (unit.CanAttack())
                {
                    unit.DoAttack1();
                }
                break;
            }
            case BattleWorldInputEventType.ATTACK2:
            {
                if (unit.CanAttack())
                {
                    unit.DoAttack2();
                }
                break;
            }
            case BattleWorldInputEventType.FIRE:
            {
                if (unit.CanAttack())
                {
                    unit.DoFire();
                }
                break;
            }
            case BattleWorldInputEventType.JUMP:
            {
                if (unit.CanJump())
                {
                    unit.DoJump();
                }
                break;
            }
        }

        eventInfo.Release();
    }

    private BattleUnit GetUnit(int unitID)
    {
        foreach (var unit in Units)
        {
            if (unit.ID != unitID) continue;
            return unit;
        }

        Debug.LogError($"Not found unit. ID:{unitID}");
        return null;
    }

    public BattleWorld Clone()
    {
        var world = WorldManager.WorldPool.Get();
        world.DeepCopyFrom(this);
        return world;
    }

    partial void OnRelease()
    {
        DisposePool();
        WorldManager.WorldPool.Release(this);
    }

    #region Pool

    [ManagedStateIgnore]
    public ObjectPool<BattleUnit> UnitPool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleUnitState> UnitStatePool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleUnitMove> UnitMovePool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleUnitJumpMove> UnitJumpMovePool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleTimer> TimerPool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleUnitJumpController> UnitJumpControllerPool { get; private set; }

    private void InitializePool()
    {
        UnitPool = new ObjectPool<BattleUnit>(() => new BattleUnit(this));
        UnitStatePool = new ObjectPool<BattleUnitState>(() => new BattleUnitState(this));
        UnitMovePool = new ObjectPool<BattleUnitMove>(() => new BattleUnitMove(this));
        UnitJumpMovePool = new ObjectPool<BattleUnitJumpMove>(() => new BattleUnitJumpMove(this));
        UnitJumpControllerPool = new ObjectPool<BattleUnitJumpController>(() => new BattleUnitJumpController(this));
        TimerPool = new ObjectPool<BattleTimer>(() => new BattleTimer(this));
    }

    private void DisposePool()
    {
        UnitPool.Dispose();
        UnitStatePool.Dispose();
        UnitMovePool.Dispose();
        UnitJumpMovePool.Dispose();
        TimerPool.Dispose();
        UnitJumpControllerPool.Dispose();
    }

    #endregion Pool
}