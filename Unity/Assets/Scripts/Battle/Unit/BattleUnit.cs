using UnityEngine;

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
    private BattleWorldSceneObjectHandle? Handle { get; set; }
    private BattleUnitState State { get; set; }
    private BattleUnitMoveController MoveController { get; set; }
    private BattleUnitJumpController JumpController { get; set; }
    private BattleUnitAttackController AttackController { get; set; }
    private BattleUnitDashMoveController HitMoveController { get; set; }
    public BattleWorldSceneAnimationSampleInfo AnimationSampleInfo;
    private float InterpolatingTime { get; set; } = 0.0f;

    public bool IsMoving() => State.StateType == BattleUnitStateType.MOVE_FORWARD || State.StateType == BattleUnitStateType.MOVE_BACK;
    public bool CanMove() => State.StateType == BattleUnitStateType.IDLE || IsMoving();
    public bool CanAttack() => State.StateType == BattleUnitStateType.IDLE || IsMoving();
    public bool CanJump() => State.StateType == BattleUnitStateType.IDLE || IsMoving();
    public BattleUnitMoveSide InputMoveSide { get; private set; }

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

        Handle = World.WorldScene?.Instantiate(BattleWorldResources.UNIT, position, rotation);
        if (Handle != null)
        {
            var sceneUnit = World.WorldScene.GetSceneUnit(Handle.Value);
            sceneUnit.Initialize(unitID);
        }

        State.SetStateInfo(BattleUnitStateInfo.IDLE);
    }

    // TODO:
    private (Vector3 DeltaPosition, Quaternion DeltaRotation) SampleAnimation(in BattleWorldSceneAnimationSampleInfo animationSampleInfo)
    {
        return (Vector3.zero, Quaternion.identity);
    }

    public void AdvanceFrame(in BattleFrame frame)
    {
        InterpolatingTime = 0.0f;
        AnimationSampleInfo = new BattleWorldSceneAnimationSampleInfo(State);

        var (animationDeltaPosition, animationDeltaRotation) = SampleAnimation(AnimationSampleInfo);
        State.AdvanceFrame(frame.DeltaTime, out var isStateChanged);

        if (isStateChanged && State.StateType == BattleUnitStateType.IDLE)
        {
            // IDLE 상태로 전환된 경우, 이동 입력에 상태에 따라 다음 상태를 판단한다.
            switch (InputMoveSide)
            {
                case BattleUnitMoveSide.RIGHT_ARROW:
                {
                    StartMoveRightArrow(true);
                    break;
                }
                case BattleUnitMoveSide.LEFT_ARROW:
                {
                    StartMoveLeftArrow(true);
                    break;
                }
            }
        }
        else if (IsMoving())
        {
            // 이동중인 경우, 캐릭터 움직임에 따른 이동 방향을 다시 판단한다.
            switch (InputMoveSide)
            {
                case BattleUnitMoveSide.RIGHT_ARROW:
                {
                    var (moveStateInfo, directionScale) = AdjustMoveRightArrow();
                    if (State.StateInfo != moveStateInfo)
                    {
                        State.SetNextStateInfo(moveStateInfo);
                        MoveController.Start(directionScale, MOVE_SPEED);
                    }
                    break;
                }
                case BattleUnitMoveSide.LEFT_ARROW:
                {
                    var (moveStateInfo, directionScale) = AdjustMoveLeftArrow();
                    if (State.StateInfo != moveStateInfo)
                    {
                        State.SetNextStateInfo(moveStateInfo);
                        MoveController.Start(directionScale, MOVE_SPEED);
                    }
                    break;
                }
            }
        }

        var stateDeltaPosition = isStateChanged ? Vector3.zero : AdvanceMoveState(frame);
        var moveDeltaPosition = animationDeltaPosition + stateDeltaPosition;

        var movePosition = Position + moveDeltaPosition;

        // 특정 상태에서 중력 연산을 적용한다.
        switch (State.StateType)
        {
            case BattleUnitStateType.IDLE:
            case BattleUnitStateType.MOVE_FORWARD:
            case BattleUnitStateType.MOVE_BACK:
            {
                if (movePosition.y > 0)
                {
                    movePosition += Physics.gravity * frame.DeltaTime;
                }
                break;
            }
        }

        // 위치가 0미만으로 떨어지지 않도록 한다.
        if (movePosition.y < 0.0f)
        {
            movePosition.y = 0.0f;
        }

        Position = movePosition;
        Rotation *= animationDeltaRotation;

        // 특정 상태에서 상대방을 바라보도록 방향을 보정한다.
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
    }

    public Vector3 PreviousPosition { get; private set; }
    public Quaternion PreviousRotation { get; private set; }

    public void Apply(BattleWorldScene scene)
    {
        InterpolatingTime = 0.0f;
        PreviousPosition = Position;
        PreviousRotation = Rotation;

        if (Handle != null)
        {
            scene.SetPositionAndRotation(Handle.Value, Position, Rotation);
            scene.SampleAnimation(Handle.Value, AnimationSampleInfo);
        }
    }

    public void Interpolate(in BattleFrame frame, BattleWorldScene worldScene)
    {
        return;
        InterpolatingTime += frame.DeltaTime;

        if (Handle != null)
        {
            var t = Mathf.Clamp01(InterpolatingTime / frame.FixedDeltaTime);
            var position = Vector3.Lerp(PreviousPosition, Position, t);
            var rotation = Quaternion.Slerp(PreviousRotation, Rotation, t);

            worldScene.SetPositionAndRotation(Handle.Value, position, rotation);

            if (AnimationSampleInfo.NextStateInfo != null &&
                AnimationSampleInfo.StateInfo is BattleUnitFiniteStateInfo finiteStateInfo &&
                AnimationSampleInfo.ElapsedTime + InterpolatingTime > finiteStateInfo.Duration)
            {
                var elasedTime = AnimationSampleInfo.ElapsedTime + InterpolatingTime - finiteStateInfo.Duration;
                AnimationSampleInfo = AnimationSampleInfo.SetNextInfo(elasedTime);
                worldScene.SampleAnimation(Handle.Value, AnimationSampleInfo);
            }
            else
            {
                worldScene.UpdateAnimation(Handle.Value, frame.DeltaTime);
            }
        }
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
                break;
            }
            case BattleUnitStateType.HIT:
            {
                if (HitMoveController.IsMoving())
                {
                    return HitMoveController.AdvanceTime(frame.DeltaTime);
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
                break;
            }
        }
    }

    public void SetInputSide(BattleUnitMoveSide moveSide)
    {
        InputMoveSide = moveSide;
    }

    public bool ResetInputSide(BattleUnitMoveSide moveSide)
    {
        if (InputMoveSide == moveSide)
        {
            InputMoveSide = BattleUnitMoveSide.NONE;
            return true;
        }
        return false;
    }

    public void StartMoveLeftArrow(bool immediately)
    {
        SetInputSide(BattleUnitMoveSide.LEFT_ARROW);
        var (moveStateInfo, directionScale) = AdjustMoveLeftArrow();
        if (immediately)
        {
            State.SetStateInfo(moveStateInfo);
        }
        else
        {
            State.SetNextStateInfo(moveStateInfo);
        }
        MoveController.Start(directionScale, MOVE_SPEED);
    }

    public void StopMoveLeftArrow()
    {
        if (ResetInputSide(BattleUnitMoveSide.LEFT_ARROW))
        {
            State.SetNextStateInfo(BattleUnitStateInfo.IDLE);
            MoveController.Stop();
        }
    }

    public void StartMoveRightArrow(bool immediately)
    {
        SetInputSide(BattleUnitMoveSide.RIGHT_ARROW);
        var (moveStateInfo, directionScale) = AdjustMoveRightArrow();
        if (immediately)
        {
            State.SetStateInfo(moveStateInfo);
        }
        else
        {
            State.SetNextStateInfo(moveStateInfo);
        }

        MoveController.Start(directionScale, MOVE_SPEED);
    }

    private (BattleUnitStateInfo StateInfo, int DirectionScale) AdjustMoveRightArrow()
    {
        var cameraLocalPosition = World.WorldManager.Camera.transform.InverseTransformPoint(Position);

        var otherUnit = World.GetOtherUnit(ID);
        var otherUnitCameraLocalPosition = World.WorldManager.Camera.transform.InverseTransformPoint(otherUnit.Position);

        if (cameraLocalPosition.x > otherUnitCameraLocalPosition.x)
        {
            return (BattleUnitStateInfo.MOVE_BACK, -1);
        }
        else
        {
            return (BattleUnitStateInfo.MOVE_FORWARD, 1);
        }
    }

    private (BattleUnitStateInfo StateInfo, int DirectionScale) AdjustMoveLeftArrow()
    {
        var cameraLocalPosition = World.WorldManager.Camera.transform.InverseTransformPoint(Position);

        var otherUnit = World.GetOtherUnit(ID);
        var otherUnitCameraLocalPosition = World.WorldManager.Camera.transform.InverseTransformPoint(otherUnit.Position);

        if (cameraLocalPosition.x > otherUnitCameraLocalPosition.x)
        {
            return (BattleUnitStateInfo.MOVE_FORWARD, 1);
        }
        else
        {
            return (BattleUnitStateInfo.MOVE_BACK, -1);
        }
    }

    public void StopMoveRightArrow()
    {
        if (ResetInputSide(BattleUnitMoveSide.RIGHT_ARROW))
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
        AttackController.Initialize(0.4f, 1.0f);
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
        // TODO: 물리
        //var sceneUnit = World.WorldScene.GetSceneUnit(Handle);
        //using var _ = ListPool<int>.Get(out var unitIds);
        //sceneUnit.GetUnitIds(unitIds);
        //foreach (var unitID in unitIds)
        //{
        //    World.PerformAttack(this, unitID);
        //}
    }

    public BattleUnit Clone(BattleWorld context)
    {
        var clone = context.UnitPool.Get();
        clone.World = context;
        clone.DeepCopyFrom(context, this);
        return clone;
    }

    partial void OnRelease(BattleWorld context)
    {
        context.UnitPool.Release(this);
    }
}
