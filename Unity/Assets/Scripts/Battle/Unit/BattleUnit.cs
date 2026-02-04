using UnityEngine;
using UnityEngine.Pool;

[ManagedState]
public partial class BattleUnit
{
    private const float MOVE_SPEED = 1.0f;

    private const float HIT_MOVE_AMOUNT = 0.5f;
    private const float HIT_MOVE_TIME = 1.167f;

    [ManagedStateIgnore]
    public BattleWorld World { get; set; }
    public int ID { get; private set; }
    public Vector3 Position { get; private set; } = Vector3.zero;
    public Vector3 NextPosition { get; private set; } = Vector3.zero;
    public Quaternion Rotation { get; private set; } = Quaternion.identity;
    public Quaternion NextRotation { get; private set; } = Quaternion.identity;

    private BattleWorldSceneObjectHandle Handle { get; set; }
    private BattleUnitState State { get; set; }
    private BattleUnitStateType AdjustStateType { get; set; }
    private BattleUnitMoveController MoveController { get; set; }
    private BattleUnitJumpController JumpController { get; set; }
    private BattleUnitAttackController AttackController { get; set; }
    private BattleUnitDashMoveController HitMoveController { get; set; }

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
        NextPosition = position;

        Rotation = rotation;
        NextRotation = rotation;

        Handle = World.WorldScene.Instantiate(BattleWorldResources.UNIT, position, rotation);
        var sceneUnit = World.WorldScene.GetSceneUnit(Handle);
        sceneUnit.Initialize(unitID);

        State.PlayAnimation(BattleUnitStateType.IDLE, BattleUnitAnimationNames.IDLE);
    }

    public void OnFixedUpdate(in BattleFrame frame)
    {
        var (animationDeltaPosition, deltaRotation) = World.WorldScene.SampleAnimation(Handle, new BattleWorldSceneAnimationSampleInfo(State));
        State.AdvanceFrame(frame.DeltaTime);

        Position = NextPosition;
        Rotation = NextRotation;

        var moveStateDeltaPosition = AdvanceMoveState(frame);
        var moveDeltaPosition = animationDeltaPosition + moveStateDeltaPosition;

        NextPosition += moveDeltaPosition;
        NextRotation *= deltaRotation;

        World.WorldScene.SetPositionAndRotation(Handle, Position, Rotation);
    }

    public void OnAfterSimulateFixedUpdate(in BattleFrame frame)
    {
        AdvanceAttackState(frame);
    }

    public void AdjustNextPositionAndRotation(in BattleFrame frame)
    {
        switch (State.StateType)
        {
            case BattleUnitStateType.IDLE:
            case BattleUnitStateType.MOVE_FORWARD:
            case BattleUnitStateType.MOVE_BACK:
            {
                (NextPosition, NextRotation) = AdjustPositionAndRotation(frame, NextPosition, NextRotation);
                break;
            }
        }
    }

    private Vector3 AdvanceMoveState(in BattleFrame frame)
    {
        AdjustStateType = State.StateType;
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

    private (Vector3 Position, Quaternion Rotation) AdjustPositionAndRotation(in BattleFrame frame, Vector3 position, Quaternion rotation)
    {
        const float ROTATION_SNAP_THRESHOLD = 0.5f;
        var otherUnit = World.GetOtherUnit(ID);
        var targetRotation = Quaternion.LookRotation(otherUnit.Position.ToXZ() - Position.ToXZ());
        if (Quaternion.Angle(rotation, targetRotation) < ROTATION_SNAP_THRESHOLD)
        {
            rotation = targetRotation;
        }
        else
        {
            rotation = Quaternion.Slerp(rotation, targetRotation, frame.DeltaTime * 6.0f);
        }

        const float POSITION_SNAP_SQR_THREADSHOLD = 0.05f * 0.05f;
        var targetPosition = position.ToXZ();
        if ((targetPosition - position).sqrMagnitude < POSITION_SNAP_SQR_THREADSHOLD)
        {
            position = targetPosition;
        }
        else
        {
            position = Vector3.Lerp(position, targetPosition, frame.DeltaTime * 6.0f);
        }

        return (position, rotation);
    }

    private void ResetState()
    {
        switch (MoveController.MoveSide)
        {
            case BattleUnitMoveSide.None:
            {
                State.PlayAnimation(BattleUnitStateType.IDLE, BattleUnitAnimationNames.IDLE);
                break;
            }
            case BattleUnitMoveSide.Forward:
            {
                State.PlayAnimation(BattleUnitStateType.MOVE_FORWARD, BattleUnitAnimationNames.MOVE_FORWARD);
                break;
            }
            case BattleUnitMoveSide.Back:
            {
                State.PlayAnimation(BattleUnitStateType.MOVE_BACK, BattleUnitAnimationNames.MOVE_BACK);
                break;
            }
        }
    }

    public void Interpolate(in BattleFrame frame)
    {
        var (animationDeltaPosition, animationDeltaRotation) = World.WorldScene.UpdateAnimation(Handle, frame.DeltaTime);
        var moveStateDeltaPosition = Vector3.zero; // InterpolateMoveState(frame);
        var deltaPosition = animationDeltaPosition + moveStateDeltaPosition;
        World.WorldScene.ApplyDeltaPositionAndRotation(Handle, deltaPosition, animationDeltaRotation);
    }

    private Vector3 InterpolateMoveState(in BattleFrame frame)
    {
        switch (AdjustStateType)
        {

            case BattleUnitStateType.MOVE_BACK:
            case BattleUnitStateType.MOVE_FORWARD:
            {
                var rotation = Quaternion.Slerp(Rotation, NextRotation, frame.DeltaTime);
                return MoveController.AdvanceTime(frame.DeltaTime, rotation);
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

    public void StartMoveBack()
    {
        State.PlayAnimation(BattleUnitStateType.MOVE_BACK, BattleUnitAnimationNames.MOVE_BACK);
        MoveController.Start(BattleUnitMoveSide.Back, MOVE_SPEED);
    }

    public void StopMoveBack()
    {
        if (State.StateType == BattleUnitStateType.MOVE_BACK)
        {
            State.PlayAnimation(BattleUnitStateType.IDLE, BattleUnitAnimationNames.IDLE);
            MoveController.Stop();
        }
    }

    public void StartMoveForward()
    {
        State.PlayAnimation(BattleUnitStateType.MOVE_FORWARD, BattleUnitAnimationNames.MOVE_FORWARD);
        MoveController.Start(BattleUnitMoveSide.Forward, MOVE_SPEED);
    }

    public void StopMoveForward()
    {
        if (State.StateType == BattleUnitStateType.MOVE_FORWARD)
        {
            State.PlayAnimation(BattleUnitStateType.IDLE, BattleUnitAnimationNames.IDLE);
            MoveController.Stop();
        }
    }

    public void DoAttack1()
    {
        AttackController.Initialize(0.233333f, 0.833f);
        State.PlayAnimation(BattleUnitStateType.ATTACK, BattleUnitAnimationNames.ATTACK1);
    }

    public void DoAttack2()
    {
        AttackController.Initialize(0.5f, 1.0f);
        State.PlayAnimation(BattleUnitStateType.ATTACK, BattleUnitAnimationNames.ATTACK2);
    }

    public void DoFire()
    {
        State.PlayAnimation(BattleUnitStateType.ATTACK, BattleUnitAnimationNames.FIRE);
    }

    public void DoHit(int damage)
    {
        HitMoveController.Initialize(Vector3.right, HIT_MOVE_AMOUNT, HIT_MOVE_TIME);
        State.PlayAnimation(BattleUnitStateType.HIT, BattleUnitAnimationNames.HIT);
    }

    public void DoJump()
    {
        JumpController.DoJump();
        State.PlayAnimation(BattleUnitStateType.JUMPING, BattleUnitAnimationNames.JUMP);
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
