using FixedMathSharp;

[ManagedState(typeof(BattleWorld))]
public partial class BattleUnitJumpController
{
    private static readonly Fixed64 JUMP_DELAY = new Fixed64(0.666666d);
    private static readonly Fixed64 JUMP_AFTER_DELAY = new Fixed64(0.813334d);
    private static readonly Fixed64 JUMP_MOVE_AMOUNT = new Fixed64(2.0d);
    private static readonly Fixed64 JUMP_MOVE_TIME = new Fixed64(0.6d);

    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    private BattleUnitJumpState State { get; set; }

    private BattleTimer JumpTimer { get; set; }
    private BattleUnitJumpMove JumpMove { get; set; }

    public bool CanJump() => !JumpTimer.IsRunning() && JumpMove.IsFinished();

    public bool IsJumping() => State != BattleUnitJumpState.NONE;

    public BattleUnitJumpController(BattleWorld world)
    {
        World = world;
        JumpTimer = world.TimerPool.Get();
        JumpMove = world.UnitJumpMovePool.Get();
    }

    public Vector3d AdvanceTime(in BattleFrame frame)
    {
        var (moveDelta, nextState) = AdvanceState(State, frame.DeltaTime);
        if (nextState != null)
        {
            switch (nextState)
            {
                case BattleUnitJumpState.NONE:
                {
                    break;
                }
                case BattleUnitJumpState.RISING:
                {
                    JumpMove.Initialize(JUMP_MOVE_AMOUNT, JUMP_MOVE_TIME);
                    break;
                }
                case BattleUnitJumpState.LANDING:
                {
                    JumpTimer.Set(JUMP_AFTER_DELAY);
                    break;
                }
            }

            State = nextState.Value;
        }

        return moveDelta;
    }

    public void DoJump()
    {
        State = BattleUnitJumpState.STARTING;
        JumpTimer.Set(JUMP_DELAY);
    }

    private (Vector3d MoveDelta, BattleUnitJumpState? NextState) AdvanceState(BattleUnitJumpState state, Fixed64 deltaTime)
    {
        switch (state)
        {
            case BattleUnitJumpState.NONE:
            {
                return (Vector3d.Zero, null);
            }
            case BattleUnitJumpState.STARTING:
            {
                var isFinished = JumpTimer.AdvanceTime(deltaTime);
                if (isFinished)
                {
                    return (Vector3d.Zero, BattleUnitJumpState.RISING);
                }
                else
                {
                    return (Vector3d.Zero, null);
                }
            }
            case BattleUnitJumpState.RISING:
            {
                return (JumpMove.AdvanceTime(deltaTime), JumpMove.IsFinished() ? BattleUnitJumpState.LANDING : null);
            }
            case BattleUnitJumpState.LANDING:
            {
                var isFinished = JumpTimer.AdvanceTime(deltaTime);
                if (isFinished)
                {
                    return (Vector3d.Zero, BattleUnitJumpState.NONE);
                }
                else
                {
                    return (Vector3d.Zero, null);
                }
            }
            default:
            {
                return (Vector3d.Zero, null);
            }
        }
    }

    public BattleUnitJumpController Clone(BattleWorld context)
    {
        var clone = context.UnitJumpControllerPool.Get();
        clone.World = context;
        clone.DeepCopyFrom(context, this);
        return clone;
    }

    partial void OnRelease(BattleWorld context)
    {
        context.UnitJumpControllerPool.Release(this);
    }
}
