using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[ManagedState]
public partial class BattleWorld
{
    private Debug Debug = new(nameof(BattleWorld));

    protected static readonly Vector3 LEFT_UNIT_START_POSITION = new(-4.5f, 0.0f, 0.0f);
    protected static readonly Quaternion LEFT_UNIT_ROTATION = Quaternion.Euler(0.0f, 90.0f, 0.0f);

    protected static readonly Vector3 RIGHT_UNIT_START_POSITION = new(4.5f, 0.0f, 0.0f);
    protected static readonly Quaternion RIGHT_UNIT_ROTATION = Quaternion.Euler(0.0f, -90.0f, 0.0f);

    public int ID { get; set; }
    public BattleWorldManager WorldManager { get; }
    public BattleWorldScene WorldScene { get; private set; }
    public int CurrentFrame { get; private set; }
    public int NextFrame => CurrentFrame + 1;
    private List<BattleWorldEventInfo> WorldEventInfos { get; set; } = new(16);

    private List<BattleUnit> Units { get; set; } = new();

    public void PerformAttack(BattleUnit attacker, int unitID)
    {
        foreach (var unit in Units)
        {
            if(unit.ID != unitID)
            {
                continue;
            }

            unit.DoHit(1);
        }
    }

    public BattleWorld(BattleWorldManager worldManager)
    {
        InitializePool();
        WorldManager = worldManager;
    }

    public void Prepare(BattleWorldScene worldScene)
    {
        WorldScene = worldScene; 
    }

    public void Initialize()
    {
        WorldScene.Initialize();
        AddUnit(0, LEFT_UNIT_START_POSITION, LEFT_UNIT_ROTATION);
        AddUnit(1, RIGHT_UNIT_START_POSITION, RIGHT_UNIT_ROTATION);
    }

    public bool IsReady()
    {
        return WorldScene.IsReady();
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

    private void AddUnit(int unitID, Vector3 position, Quaternion rotation)
    {
        var unit = UnitPool.Get();
        unit.Initialize(unitID, position, rotation);
        Units.Add(unit);
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

    public Rect GetRectContainingUnits()
    {
        var xMin = float.MaxValue;
        var xMax = float.MinValue;
        var yMax = 0.0f;
        foreach (var unit in Units)
        {
            xMin = Mathf.Min(xMin, unit.Position.x - 1.0f);
            xMax = Mathf.Max(xMax, unit.Position.x + 1.0f);
            yMax = Mathf.Max(yMax, unit.Position.y + 1.0f);
        }

        return Rect.MinMaxRect(xMin, 0.0f, xMax, yMax);
    }

    #region Pool

    [ManagedStateIgnore]
    public ObjectPool<BattleUnit> UnitPool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleUnitState> UnitStatePool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleUnitMoveController> UnitMoveControllerPool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleUnitJumpMove> UnitJumpMovePool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleTimer> TimerPool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleUnitJumpController> UnitJumpControllerPool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleUnitAttackController> UnitAttackControllerPool { get; private set; }

    private void InitializePool()
    {
        UnitPool = new ObjectPool<BattleUnit>(() => new BattleUnit(this));
        UnitStatePool = new ObjectPool<BattleUnitState>(() => new BattleUnitState(this));
        UnitMoveControllerPool = new ObjectPool<BattleUnitMoveController>(() => new BattleUnitMoveController(this));
        UnitJumpMovePool = new ObjectPool<BattleUnitJumpMove>(() => new BattleUnitJumpMove(this));
        UnitJumpControllerPool = new ObjectPool<BattleUnitJumpController>(() => new BattleUnitJumpController(this));
        UnitAttackControllerPool = new ObjectPool<BattleUnitAttackController>(() => new BattleUnitAttackController(this));
        TimerPool = new ObjectPool<BattleTimer>(() => new BattleTimer(this));
    }

    private void DisposePool()
    {
        UnitPool.Dispose();
        UnitStatePool.Dispose();
        UnitMoveControllerPool.Dispose();
        UnitJumpMovePool.Dispose();
        UnitJumpControllerPool.Dispose();
        TimerPool.Dispose();
    }

    #endregion Pool
}