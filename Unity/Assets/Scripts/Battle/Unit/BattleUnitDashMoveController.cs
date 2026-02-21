using FixedMathSharp;

[ManagedState(typeof(BattleWorld))]
public partial class BattleUnitDashMoveController
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }
    public Vector3d Direction { get; private set; }

    public Fixed64 Velocity { get; private set; }
    public Fixed64 Deceleration { get; private set; }
    public Fixed64 MoveTime { get; private set; }
    public Fixed64 ElapsedTime { get; private set; }

    public BattleUnitDashMoveController(BattleWorld world)
    {
        World = world;
    }

    public void Initialize(Vector3d direction, Fixed64 distance, Fixed64 time)
    {
        Direction = direction;
        Velocity = distance / time * 2;
        Deceleration = Velocity / time;
        MoveTime = time;
        ElapsedTime = Fixed64.Zero;
    }

    public Vector3d AdvanceTime(Fixed64 deltaTime)
    {
        if (ElapsedTime + deltaTime > MoveTime)
        {
            deltaTime = MoveTime - ElapsedTime;
            ElapsedTime = MoveTime;
        }
        else
        {
            ElapsedTime += deltaTime;
        }

        // 일정 시간동안 일정 거리만큼 특정 방향으로 이동한다.
        // 속도는 중력가속도 법칙에 의해 줄어든다.
        var moveDelta = (Velocity - (Deceleration * deltaTime * new Fixed64(0.5f))) * deltaTime;
        Velocity -= Deceleration * deltaTime;
        return moveDelta * Direction;
    }

    public bool IsMoving()
    {
        return ElapsedTime != MoveTime;
    }

    public BattleUnitDashMoveController Clone(BattleWorld context)
    {
        var clone = context.UnitDashMoveControllerPool.Get();
        clone.World = context;
        clone.DeepCopyFrom(context, this);
        return clone;
    }

    partial void OnRelease(BattleWorld context)
    {
        context.UnitDashMoveControllerPool.Release(this);
    }
}
