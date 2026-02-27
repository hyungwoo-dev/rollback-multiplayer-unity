using FixedMathSharp;
using System.Collections.Generic;
using System.Text;

public partial class BattleWorld
{
    private Debug Debug = new(nameof(BattleWorld));

    protected static readonly Vector3d LEFT_UNIT_START_POSITION = new(-4.5d, 0.0d, 0.0d);
    protected static readonly FixedQuaternion LEFT_UNIT_ROTATION = FixedQuaternion.FromEulerAngles(new Fixed64(0.0).ToRadians(), new Fixed64(90.0d).ToRadians(), new Fixed64(0.0d).ToRadians());

    protected static readonly Vector3d RIGHT_UNIT_START_POSITION = new(4.5d, 0.0d, 0.0d);
    protected static readonly FixedQuaternion RIGHT_UNIT_ROTATION = FixedQuaternion.FromEulerAngles(new Fixed64(0.0d).ToRadians(), new Fixed64(-90.0d).ToRadians(), new Fixed64(0.0d).ToRadians());

    protected static readonly Vector3d CAMERA_START_POSITION = new(0.0d, 1.0d, -9.1d);
    public static readonly FixedQuaternion CAMERA_START_ROTATION = FixedQuaternion.FromEulerAngles(new Fixed64(3.0d).ToRadians(), Fixed64.Zero.ToRadians(), Fixed64.Zero.ToRadians());

    public BaseWorldManager WorldManager { get; private set; }
    public BattleWorldScene WorldScene { get; private set; }
    public int CurrentFrame;
    public int NextFrame => CurrentFrame + 1;

    private List<BattleUnit> Units { get; set; } = new();

    public BattleCameraTransform CameraTransform { get; private set; } = new();

    public int GetWorldHash()
    {
        var hash = int.MaxValue;
        foreach (var unit in Units)
        {
            hash ^= unit.GetUnitHash();
        }
        hash ^= CameraTransform.GetCameraHash();
        return hash;
    }

    public void ReleaseWorldEventInfos(List<BattleWorldEventInfo> worldEventInfos)
    {
        foreach (var worldEventInfo in worldEventInfos)
        {
            worldEventInfo.Release(this);
        }

        WorldEventInfoListPool.Release(worldEventInfos);
    }

    public void PerformAttack(BattleUnit attacker, int unitID, Fixed64 knockbackAmount, Fixed64 knockbackDuration)
    {
        foreach (var unit in Units)
        {
            if (unit.ID != unitID)
            {
                continue;
            }

            unit.DoHit(1, knockbackAmount, knockbackDuration);
        }
    }

    public void ApplyTo(BattleWorldScene worldScene)
    {
        foreach (var unit in Units)
        {
            unit.Apply(worldScene);
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

    public string GetInfo()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"CurrentFrame: {CurrentFrame}");
        foreach (var unit in Units)
        {
            stringBuilder.AppendLine($"UnitID: {unit.ID}, Position: {unit.Position}, Rotation: {unit.Rotation}");
        }
        return stringBuilder.ToString();
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
        AddUnit(0, LEFT_UNIT_START_POSITION, LEFT_UNIT_ROTATION);
        AddUnit(1, RIGHT_UNIT_START_POSITION, RIGHT_UNIT_ROTATION);
        CameraTransform.Initialize(this, CAMERA_START_POSITION, CAMERA_START_ROTATION);
    }

    public void Interpolate(in BattleFrame frame, BattleWorldScene worldScene)
    {
        foreach (var unit in Units)
        {
            unit.Interpolate(frame, worldScene);
        }
    }

    public void AdvanceFrame(in BattleFrame frame)
    {
        CurrentFrame += 1;

        foreach (var unit in Units)
        {
            unit.AdvanceFrame(frame);
        }

        var unit1 = Units[0];
        var unit2 = Units[1];
        var unit1Circle = BattleCircle.FromUnit(unit1);
        var unit2Circle = BattleCircle.FromUnit(unit2);
        if (BattleCircle.CheckCollision(unit1Circle, unit2Circle, out var distance))
        {
            var direction = (unit2.Position - unit1.Position).Normalize();
            var adjustVector = direction * distance * Fixed64.Half;
            unit1.Position -= adjustVector;
            unit2.Position += adjustVector;
        }

        foreach (var unit in Units)
        {
            unit.CheckCollisions(frame);
        }

        CameraTransform.AdvanceFrame(frame);
    }

    private void AddUnit(int unitID, Vector3d position, FixedQuaternion rotation)
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

        CameraTransform.DeepCopyFrom(this, other.CameraTransform);
    }

    public void Release()
    {
        CurrentFrame = 0;

        foreach (var unit in Units)
        {
            unit.Release(this);
        }
        Units.Clear();

        CameraTransform.Release(this);

        WorldManager.WorldPool.Release(this);
    }

    public void Dispose()
    {
        Release();
        DisposePool();
    }

    public (Vector3d TargetPosition, FixedQuaternion TargetRotation) GetCameraTargetPositionAndRotation(in Fixed4x4 cameraTransform)
    {
        Fixed64 CAMERA_DISTANCE_MIN = new Fixed64(5.0d);
        var unit1 = Units[0];
        var unit2 = Units[1];

        var averagePosition = (unit1.Position + unit2.Position) * new Fixed64(0.5d);

        var cameraLocalPosition1 = cameraTransform.InverseTransformPoint(unit1.Position);
        var cameraLocalPosition2 = cameraTransform.InverseTransformPoint(unit2.Position);
        var rightUnitPosition = cameraLocalPosition1.x > cameraLocalPosition2.x ? unit1.Position : unit2.Position;
        var rightUnitLocalPosition = rightUnitPosition - averagePosition;

        var cameraDistance = MathUtils.Max((unit2.Position - unit1.Position).Magnitude * new Fixed64(0.9d), CAMERA_DISTANCE_MIN);
        var cameraForward = Vector3d.Cross(new Vector3d(rightUnitLocalPosition.x, new Fixed64(0.0d), rightUnitLocalPosition.z), Vector3d.Up).Normalize();

        var cameraTargetRotation = FixedQuaternion.LookRotation(cameraForward) * CAMERA_START_ROTATION;
        var cameraTargetPosition = (averagePosition + cameraForward * -cameraDistance) + Vector3d.Up;
        return (cameraTargetPosition, cameraTargetRotation);
    }

    #region Pool

    [ManagedStateIgnore]
    public Pool<BattleUnit> UnitPool { get; private set; }

    [ManagedStateIgnore]
    public Pool<BattleUnitState> UnitStatePool { get; private set; }

    [ManagedStateIgnore]
    public Pool<BattleUnitMoveController> UnitMoveControllerPool { get; private set; }

    [ManagedStateIgnore]
    public Pool<BattleUnitDashMoveController> UnitDashMoveControllerPool { get; private set; }

    [ManagedStateIgnore]
    public Pool<BattleTimer> TimerPool { get; private set; }

    [ManagedStateIgnore]
    public Pool<BattleUnitAttackController> UnitAttackControllerPool { get; private set; }

    [ManagedStateIgnore]
    public Pool<BattleWorldEventInfo> WorldEventInfoPool { get; private set; }

    [ManagedStateIgnore]
    public Pool<List<BattleWorldEventInfo>> WorldEventInfoListPool { get; private set; }

    private void InitializePool()
    {
        UnitPool = new Pool<BattleUnit>(() => new BattleUnit(this));
        UnitStatePool = new Pool<BattleUnitState>(() => new BattleUnitState(this));
        UnitMoveControllerPool = new Pool<BattleUnitMoveController>(() => new BattleUnitMoveController(this));
        UnitDashMoveControllerPool = new Pool<BattleUnitDashMoveController>(() => new BattleUnitDashMoveController(this));
        UnitAttackControllerPool = new Pool<BattleUnitAttackController>(() => new BattleUnitAttackController(this));
        TimerPool = new Pool<BattleTimer>(() => new BattleTimer(this));
        WorldEventInfoPool = new Pool<BattleWorldEventInfo>(createFunc: () => new BattleWorldEventInfo(this));
        WorldEventInfoListPool = new Pool<List<BattleWorldEventInfo>>(() => new List<BattleWorldEventInfo>(), list => list.Clear());
    }

    private void DisposePool()
    {
        UnitPool.Dispose();
        UnitStatePool.Dispose();
        UnitMoveControllerPool.Dispose();
        UnitDashMoveControllerPool.Dispose();
        TimerPool.Dispose();
        WorldEventInfoPool.Dispose();
        WorldEventInfoListPool.Dispose();
    }

    #endregion Pool
}