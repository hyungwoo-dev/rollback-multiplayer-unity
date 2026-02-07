using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[ManagedState(typeof(BattleWorldManager))]
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
            if (unit.ID != unitID)
            {
                continue;
            }

            unit.DoHit(1);
        }
    }

    public BattleUnit GetUnit(int unitID)
    {
        foreach (var unit in Units)
        {
            if (unit.ID != unitID)
            {
                continue;
            }

            return unit;
        }
        return null;
    }

    public BattleUnit GetOtherUnit(int unitID)
    {
        foreach (var unit in Units)
        {
            if (unit.ID == unitID)
            {
                continue;
            }

            return unit;
        }
        return null;
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

    public void Interpolate(in BattleFrame frame, BattleWorld futureWorld)
    {
        foreach (var unit in Units)
        {
            unit.Interpolate(frame, futureWorld);
        }
    }

    public void AdvanceFrame(in BattleFrame frame, out bool executeWorldEventInfos)
    {
        CurrentFrame += 1;
        executeWorldEventInfos = ExecuteWorldEventInfos(CurrentFrame);

        foreach (var unit in Units)
        {
            unit.AdvanceFrame(frame);
        }

        WorldScene.SimulatePhysics(frame.DeltaTime);

        foreach (var unit in Units)
        {
            unit.OnAfterSimulateFixedUpdate(frame);
        }
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

    private bool ExecuteWorldEventInfos(int targetFrame)
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
        return popEventCount > 0;
    }

    private void ExecuteWorldEvent(BattleWorldEventInfo eventInfo)
    {
        var unit = GetUnit(eventInfo.UnitID);
        switch (eventInfo.WorldInputEventType)
        {
            case BattleWorldInputEventType.MOVE_BACK_DOWN:
            {
                if (unit.CanMove())
                {
                    unit.StartMoveBack();
                }
                break;
            }
            case BattleWorldInputEventType.MOVE_BACK_UP:
            {
                if (unit.IsMoving())
                {
                    unit.StopMoveBack();
                }
                break;
            }
            case BattleWorldInputEventType.MOVE_FORWARD_DOWN:
            {
                if (unit.CanMove())
                {
                    unit.StartMoveForward();
                }
                break;
            }
            case BattleWorldInputEventType.MOVE_FORWARD_UP:
            {
                if (unit.IsMoving())
                {
                    unit.StopMoveForward();
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
                    unit.DoAttack3();
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

    public (Vector3 TargetPosition, Quaternion TargetRotation) GetCameraTargetPositionAndRotation(Transform cameraTransform)
    {
        const float CAMERA_DISTANCE_MIN = 5.0f;
        var unit1 = Units[0];
        var unit2 = Units[1];

        var averagePosition = (unit1.Position + unit2.Position) * 0.5f;
        var cameraLocalPosition1 = cameraTransform.InverseTransformPoint(unit1.Position);
        var cameraLocalPosition2 = cameraTransform.InverseTransformPoint(unit2.Position);
        var rightUnitPosition = cameraLocalPosition1.x > cameraLocalPosition2.x ? unit1.Position : unit2.Position;
        var rightUnitLocalPosition = rightUnitPosition - averagePosition;

        var cameraDistance = Mathf.Max((unit2.Position - unit1.Position).magnitude * 0.9f, CAMERA_DISTANCE_MIN);
        var cameraForward = Vector3.Cross(new Vector3(rightUnitLocalPosition.x, 0.0f, rightUnitLocalPosition.z), Vector3.up).normalized;

        var cameraTargetRotation = Quaternion.LookRotation(cameraForward) * Quaternion.Euler(3.0f, 0.0f, 0.0f);
        var cameraTargetPosition = (averagePosition + cameraForward * -cameraDistance) + new Vector3(0.0f, 1.0f, 0.0f);
        return (cameraTargetPosition, cameraTargetRotation);
    }

    #region Pool

    [ManagedStateIgnore]
    public ObjectPool<BattleUnit> UnitPool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleUnitState> UnitStatePool { get; private set; }

    [ManagedStateIgnore]
    public ObjectPool<BattleUnitMoveController> UnitMoveControllerPool { get; private set; }
    [ManagedStateIgnore]
    public ObjectPool<BattleUnitDashMoveController> UnitDashMoveControllerPool { get; private set; }

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
        UnitDashMoveControllerPool = new ObjectPool<BattleUnitDashMoveController>(() => new BattleUnitDashMoveController(this));
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
        UnitDashMoveControllerPool.Dispose();
        UnitJumpMovePool.Dispose();
        UnitJumpControllerPool.Dispose();
        TimerPool.Dispose();
    }

    #endregion Pool
}