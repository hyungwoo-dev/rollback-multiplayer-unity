using FixedMathSharp;

[ManagedState(typeof(BattleWorld))]
public partial class BattleUnitJumpMove
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    public Fixed64 Velocity { get; private set; }
    public Fixed64 Deceleration { get; private set; }
    public Fixed64 MoveTime { get; private set; }
    public Fixed64 ElapsedTime { get; private set; }

    public BattleUnitJumpMove(BattleWorld world)
    {
        World = world;
    }

    public void Initialize(Fixed64 distance, Fixed64 time)
    {
        var riseTime = time * Fixed64.Half;
        Velocity = distance / riseTime * 2;
        Deceleration = Velocity / riseTime;
        MoveTime = time;
        ElapsedTime = Fixed64.Zero;
    }

    public Vector3d AdvanceTime(Fixed64 deltaTime)
    {
        if (ElapsedTime + deltaTime >= MoveTime)
        {
            deltaTime = MoveTime - ElapsedTime;
            var moveDelta = GetMovedAmount(ElapsedTime + deltaTime) - GetMovedAmount(ElapsedTime);
            ElapsedTime = MoveTime;
            return moveDelta * Vector3d.Up;
        }
        else
        {
            var moveDelta = GetMovedAmount(ElapsedTime + deltaTime) - GetMovedAmount(ElapsedTime);
            ElapsedTime += deltaTime;
            return moveDelta * Vector3d.Up;
        }
    }

    private Fixed64 GetMovedAmount(Fixed64 time)
    {
        return (Velocity * time) - (Deceleration / 2 * time * time);
    }

    public bool IsFinished()
    {
        return ElapsedTime == MoveTime;
    }

    public BattleUnitJumpMove Clone(BattleWorld context)
    {
        var clone = World.UnitJumpMovePool.Get();
        clone.World = context;
        clone.DeepCopyFrom(context, this);
        return clone;
    }

    partial void OnRelease(BattleWorld context)
    {
        context.UnitJumpMovePool.Release(this);
    }
}