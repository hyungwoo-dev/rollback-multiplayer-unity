using UnityEngine;

[ManagedState]
public partial class BattleUnitJumpController
{
    private const float JUMP_DELAY = 0.666666f;
    private const float JUMP_AFTER_DELAY = 0.813334f;
    private const float JUMP_MOVE_AMOUNT = 2.0f;
    private const float JUMP_MOVE_TIME = 0.6f;

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

    public Vector3 AdvanceTime(in BattleFrame frame)
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

    private (Vector3 MoveDelta, BattleUnitJumpState? NextState) AdvanceState(BattleUnitJumpState state, float deltaTime)
    {
        switch (state)
        {
            case BattleUnitJumpState.NONE:
            {
                return (Vector3.zero, null);
            }
            case BattleUnitJumpState.STARTING:
            {
                var isFinished = JumpTimer.AdvanceTime(deltaTime);
                if (isFinished)
                {
                    return (Vector3.zero, BattleUnitJumpState.RISING);
                }
                else
                {
                    return (Vector3.zero, null);
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
                    return (Vector3.zero, BattleUnitJumpState.NONE);
                }
                else
                {
                    return (Vector3.zero, null);
                }
            }
            default:
            {
                return (Vector3.zero, null);
            }
        }
    }

    public BattleUnitJumpController Clone()
    {
        var clone = World.UnitJumpControllerPool.Get();
        clone.DeepCopyFrom(this);
        return clone;
    }

    partial void OnRelease()
    {
        World.UnitJumpControllerPool.Release(this);
    }
}
