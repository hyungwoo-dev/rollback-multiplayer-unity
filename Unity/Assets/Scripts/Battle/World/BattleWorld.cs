using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public partial class BattleWorld
{
    private Debug Debug = new(nameof(BattleWorld));

    protected static readonly Vector3 LEFT_UNIT_START_POSITION = new(-4.5f, 0.0f, 0.0f);
    protected static readonly Quaternion LEFT_UNIT_ROTATION = Quaternion.Euler(0.0f, 90.0f, 0.0f);

    protected static readonly Vector3 RIGHT_UNIT_START_POSITION = new(4.5f, 0.0f, 0.0f);
    protected static readonly Quaternion RIGHT_UNIT_ROTATION = Quaternion.Euler(0.0f, -90.0f, 0.0f);

    public BaseWorldManager WorldManager { get; private set; }
    public BattleWorldScene WorldScene { get; private set; }
    public int CurrentFrame { get; private set; }
    public int NextFrame => CurrentFrame + 1;

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

    public BattleWorld(BaseWorldManager worldManager)
    {
        InitializePool();
        WorldManager = worldManager;
    }

    public void SetWorldScene(BattleWorldScene worldScene)
    {
        WorldScene = worldScene;
    }

    public void Initialize()
    {
        WorldScene.Initialize();
        AddUnit(0, LEFT_UNIT_START_POSITION, LEFT_UNIT_ROTATION);
        AddUnit(1, RIGHT_UNIT_START_POSITION, RIGHT_UNIT_ROTATION);
    }

    public bool IsSceneLoaded()
    {
        return WorldScene.IsSceneLoaded();
    }

    public void Interpolate(in BattleFrame frame, BattleWorld futureWorld)
    {
        foreach (var unit in Units)
        {
            unit.Interpolate(frame, futureWorld);
        }
    }

    public void Apply(BattleWorld other)
    {
        CurrentFrame = other.CurrentFrame;
        foreach (var unit in Units)
        {
            unit.Apply(other);
        }
    }

    public void AdvanceFrame(in BattleFrame frame)
    {
        CurrentFrame += 1;

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

    private void AddUnit(int unitID, Vector3 position, Quaternion rotation)
    {
        var unit = UnitPool.Get();
        unit.Initialize(unitID, position, rotation);
        Units.Add(unit);
    }

    public void ExecuteWorldEventInfos(List<BattleWorldEventInfo> worldEventInfos)
    {
        for (int i = 0; i < worldEventInfos.Count; i++)
        {
            var eventInfo = worldEventInfos[i];
            ExecuteWorldEventInfo(eventInfo);
        }
    }

    public void ExecuteWorldEventInfo(BattleWorldEventInfo eventInfo)
    {
        var unit = GetUnit(eventInfo.UnitID);
        switch (eventInfo.WorldInputEventType)
        {
            case BattleWorldInputEventType.MOVE_LEFT_ARROW_DOWN:
            {
                if (unit.CanMove())
                {
                    unit.StartMoveLeftArrow(false);
                }
                else
                {
                    unit.SetInputSide(BattleUnitMoveSide.LEFT_ARROW);
                }
                break;
            }
            case BattleWorldInputEventType.MOVE_LEFT_ARROW_UP:
            {
                if (unit.IsMoving())
                {
                    unit.StopMoveLeftArrow();
                }
                else
                {
                    unit.ResetInputSide(BattleUnitMoveSide.LEFT_ARROW);
                }
                break;
            }
            case BattleWorldInputEventType.MOVE_RIGHT_ARROW_DOWN:
            {
                if (unit.CanMove())
                {
                    unit.StartMoveRightArrow(false);
                }
                else
                {
                    unit.SetInputSide(BattleUnitMoveSide.RIGHT_ARROW);
                }
                break;
            }
            case BattleWorldInputEventType.MOVE_RIGHT_ARROW_UP:
            {
                if (unit.IsMoving())
                {
                    unit.StopMoveRightArrow();
                }
                else
                {
                    unit.ResetInputSide(BattleUnitMoveSide.RIGHT_ARROW);
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
    }

    public void CopyFrom(BattleWorld other)
    {
        CurrentFrame = other.CurrentFrame;

        Units.Clear();
        foreach (var unit in other.Units)
        {
            var clone = unit.Clone(this);
            Units.Add(clone);
        }
    }

    public void Reset()
    {
        foreach (var unit in Units)
        {
            unit.ResetPositionAndRotation();
        }
    }

    public void Release()
    {
        foreach (var unit in Units)
        {
            unit.Release(this);
        }
        Units.Clear();

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