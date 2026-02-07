using UnityEngine;
using UnityEngine.Pool;

[ManagedState(typeof(BattleWorld))]
public partial class BattleUnit
{
    private const float MOVE_SPEED = 1.0f;

    private const float HIT_MOVE_AMOUNT = 0.5f;
    private const float HIT_MOVE_TIME = 1.167f;



    [ManagedStateIgnore]
    public BattleWorld World { get; set; }
    public int ID { get; private set; }
    public Vector3 Position { get; private set; } = Vector3.zero;
    public Quaternion Rotation { get; private set; } = Quaternion.identity;

    private BattleWorldSceneObjectHandle Handle { get; set; }
    private BattleUnitState State { get; set; }
    private BattleUnitMoveController MoveController { get; set; }
    private BattleUnitJumpController JumpController { get; set; }
    private BattleUnitAttackController AttackController { get; set; }
    private BattleUnitDashMoveController HitMoveController { get; set; }

    private float InterpolatingTime { get; set; } = 0.0f;

    public bool IsMoving() => State.StateType == BattleUnitStateType.MOVE_FORWARD || State.StateType == BattleUnitStateType.MOVE_BACK;
    public bool CanMove() => State.StateType == BattleUnitStateType.IDLE || IsMoving();
    public bool CanAttack() => State.StateType == BattleUnitStateType.IDLE || IsMoving();
    public bool CanJump() => State.StateType == BattleUnitStateType.IDLE || IsMoving();

    public BattleUnit(BattleWorld world)
    {
        World = world;
        State = world.UnitStatePool.Get();
        MoveController = world.UnitMoveControllerPool.Get();
        JumpController = world.UnitJumpControllerPool.Get();
        AttackController = world.UnitAttackControllerPool.Get();
        HitMoveController = world.UnitDashMoveControllerPool.Get();
    }

    public void Initialize(int unitID, Vector3 position, Quaternion rotation)
    {
        ID = unitID;

        Position = position;
        Rotation = rotation;

        Handle = World.WorldScene.Instantiate(BattleWorldResources.UNIT, position, rotation);
        var sceneUnit = World.WorldScene.GetSceneUnit(Handle);
        sceneUnit.Initialize(unitID);

        State.SetStateInfo(BattleUnitStateInfo.IDLE);
    }

    public void AdvanceFrame(in BattleFrame frame)
    {
        InterpolatingTime = 0.0f;

        var animationSampleInfo = new BattleWorldSceneAnimationSampleInfo(State);

        var (animationDeltaPosition, animationDeltaRotation) = World.WorldScene.SampleAnimation(Handle, animationSampleInfo);
        State.AdvanceFrame(frame.DeltaTime, out var isStateChanged);
        var stateDeltaPosition = isStateChanged ? Vector3.zero : AdvanceMoveState(frame);
        var moveDeltaPosition = animationDeltaPosition + stateDeltaPosition;

        var movePosition = Position + moveDeltaPosition;
        World.WorldScene.SetPositionAndRotation(Handle, Position, Rotation);
        World.WorldScene.MovePosition(Handle, movePosition);
        
        Rotation *= animationDeltaRotation;
        switch (State.StateType)
        {
            case BattleUnitStateType.IDLE:
            case BattleUnitStateType.MOVE_FORWARD:
            case BattleUnitStateType.MOVE_BACK:
            {
                Rotation = LookOtherUnitRotation(frame);
                break;
            }
        }
    }

    public void OnAfterSimulateFixedUpdate(in BattleFrame frame)
    {
        AdvanceAttackState(frame);
        if (World.WorldScene.TryGetPosition(Handle, out var position))
        {
            Position = position;
        }
    }

    public void Interpolate(in BattleFrame frame, BattleWorld futureWorld)
    {
        InterpolatingTime += frame.DeltaTime;

        var futureUnit = futureWorld.GetUnit(ID);
        var nextPosition = futureUnit.Position;
        var nextRotation = futureUnit.Rotation;
        var t = Mathf.Clamp01(InterpolatingTime / frame.FixedDeltaTime);
        var position = Vector3.Lerp(Position, nextPosition, t);
        var rotation = Quaternion.Slerp(Rotation, nextRotation, t);
        World.WorldScene.SetPositionAndRotation(Handle, position, rotation);
        World.WorldScene.UpdateAnimation(Handle, frame.DeltaTime);
    }

    private Quaternion LookOtherUnitRotation(in BattleFrame frame)
    {
        const float ROTATION_SNAP_THRESHOLD = 0.5f;
        var otherUnit = World.GetOtherUnit(ID);
        var targetRotation = Quaternion.LookRotation(otherUnit.Position.ToXZ() - Position.ToXZ());
        if (Quaternion.Angle(Rotation, targetRotation) < ROTATION_SNAP_THRESHOLD)
        {
            return targetRotation;
        }
        else
        {
            return Quaternion.Slerp(Rotation, targetRotation, frame.DeltaTime * 6.0f);
        }
    }

    private Vector3 AdvanceMoveState(in BattleFrame frame)
    {
        switch (State.StateType)
        {
            case BattleUnitStateType.MOVE_BACK:
            case BattleUnitStateType.MOVE_FORWARD:
            {
                return MoveController.AdvanceTime(frame.DeltaTime, Rotation);
            }
            case BattleUnitStateType.JUMPING:
            {
                if (JumpController.IsJumping())
                {
                    return JumpController.AdvanceTime(frame);
                }
                else
                {
                    ResetState();
                }
                break;
            }
            case BattleUnitStateType.HIT:
            {
                if (HitMoveController.IsMoving())
                {
                    return HitMoveController.AdvanceTime(frame.DeltaTime);
                }
                else
                {
                    ResetState();
                }
                break;
            }
        }

        return Vector3.zero;
    }

    private void AdvanceAttackState(in BattleFrame frame)
    {
        switch (State.StateType)
        {
            case BattleUnitStateType.ATTACK:
            {
                if (AttackController.IsAttacking())
                {
                    AttackController.AdvanceTime(frame, out var performAttack);
                    if (performAttack)
                    {
                        PerformAttack();
                    }
                }
                else
                {
                    ResetState();
                }
                break;
            }
        }
    }

    private void ResetState()
    {
        switch (MoveController.MoveSide)
        {
            case BattleUnitMoveSide.NONE:
            {
                State.SetNextStateInfo(BattleUnitStateInfo.IDLE);
                break;
            }
            case BattleUnitMoveSide.FORWARD:
            {
                State.SetNextStateInfo(BattleUnitStateInfo.MOVE_FORWARD);
                break;
            }
            case BattleUnitMoveSide.BACK:
            {
                State.SetNextStateInfo(BattleUnitStateInfo.MOVE_BACK);
                break;
            }
        }
    }

    public void StartMoveBack()
    {
        State.SetNextStateInfo(BattleUnitStateInfo.MOVE_BACK);
        MoveController.Start(BattleUnitMoveSide.BACK, MOVE_SPEED);
    }

    public void StopMoveBack()
    {
        if (State.StateType == BattleUnitStateType.MOVE_BACK)
        {
            State.SetNextStateInfo(BattleUnitStateInfo.IDLE);
            MoveController.Stop();
        }
    }

    public void StartMoveForward()
    {
        State.SetNextStateInfo(BattleUnitStateInfo.MOVE_FORWARD);
        MoveController.Start(BattleUnitMoveSide.FORWARD, MOVE_SPEED);
    }

    public void StopMoveForward()
    {
        if (State.StateType == BattleUnitStateType.MOVE_FORWARD)
        {
            State.SetNextStateInfo(BattleUnitStateInfo.IDLE);
            MoveController.Stop();
        }
    }

    public void DoAttack1()
    {
        AttackController.Initialize(0.233333f, 0.833f);
        State.SetNextStateInfo(BattleUnitStateInfo.ATTACK1);
    }

    public void DoAttack2()
    {
        AttackController.Initialize(0.5f, 1.0f);
        State.SetNextStateInfo(BattleUnitStateInfo.ATTACK2);
    }

    public void DoAttack3()
    {
        AttackController.Initialize(0.5f, 1.0f);
        State.SetNextStateInfo(BattleUnitStateInfo.ATTACK2);
    }

    public void DoHit(int damage)
    {
        HitMoveController.Initialize(Vector3.right, HIT_MOVE_AMOUNT, HIT_MOVE_TIME);
        State.SetNextStateInfo(BattleUnitStateInfo.HIT);
    }

    public void DoJump()
    {
        JumpController.DoJump();
        State.SetNextStateInfo(BattleUnitStateInfo.JUMP);
    }

    public void PerformAttack()
    {
        var sceneUnit = World.WorldScene.GetSceneUnit(Handle);
        using var _ = ListPool<int>.Get(out var unitIds);
        sceneUnit.GetUnitIds(unitIds);
        foreach (var unitID in unitIds)
        {
            World.PerformAttack(this, unitID);
        }
    }

    public BattleUnit Clone()
    {
        var clone = World.UnitPool.Get();
        clone.DeepCopyFrom(this);
        return clone;
    }

    partial void OnRelease()
    {
        World.UnitPool.Release(this);
    }
}
