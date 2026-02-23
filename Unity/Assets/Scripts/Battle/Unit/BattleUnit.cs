using FixedMathSharp;
using UnityEngine;

[ManagedState(typeof(BattleWorld))]
public partial class BattleUnit
{
    private static readonly Fixed64 MOVE_SPEED = new Fixed64(1.0d);

    [ManagedStateIgnore]
    public BattleWorld World { get; set; }
    public int ID { get; private set; }

    public Vector3d PreviousPosition { get; private set; }
    public FixedQuaternion PreviousRotation { get; private set; }

    public Vector3d Position { get; set; } = Vector3d.Zero;
    public FixedQuaternion Rotation { get; private set; } = FixedQuaternion.Identity;

    private BattleUnitState State { get; set; }
    private BattleUnitMoveController MoveController { get; set; }
    private BattleUnitAttackController AttackController { get; set; }
    private BattleUnitDashMoveController HitMoveController { get; set; }
    public BattleWorldSceneAnimationSampleInfo AnimationSampleInfo;
    private Fixed64 InterpolatingTime { get; set; } = new Fixed64(0.0d);

    public bool IsMoving() => State.StateType == BattleUnitStateType.MOVE_FORWARD || State.StateType == BattleUnitStateType.MOVE_BACK;
    public bool CanMove() => State.StateType == BattleUnitStateType.IDLE || IsMoving();
    public bool CanAttack() => State.StateType == BattleUnitStateType.IDLE || IsMoving();
    public BattleUnitMoveSide InputMoveSide { get; private set; }

    public BattleUnit(BattleWorld world)
    {
        World = world;
        State = world.UnitStatePool.Get();
        MoveController = world.UnitMoveControllerPool.Get();
        AttackController = world.UnitAttackControllerPool.Get();
        HitMoveController = world.UnitDashMoveControllerPool.Get();
    }

    public void Initialize(int unitID, Vector3d position, FixedQuaternion rotation)
    {
        ID = unitID;

        Position = position;
        Rotation = rotation;

        World.WorldScene?.InstantiateUnit(unitID, position, rotation);
        State.SetStateInfo(BattleUnitStateInfo.IDLE);
    }

    private (Vector3d DeltaPosition, FixedQuaternion DeltaRotation) SampleAnimation(in BattleWorldSceneAnimationSampleInfo animationSampleInfo)
    {
        var (deltaPosition, deltaRotation) = AnimationDeltaInfos.Shared.GetDeltas(animationSampleInfo.AnimationName, animationSampleInfo.ElapsedFrame);
        return (deltaPosition * Rotation, deltaRotation);
    }

    public void AdvanceFrame(in BattleFrame frame)
    {
        InterpolatingTime = Fixed64.Zero;
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

        var stateDeltaPosition = isStateChanged ? Vector3d.Zero : AdvanceMoveState(frame);
        var moveDeltaPosition = animationDeltaPosition + stateDeltaPosition;

        var movePosition = Position + moveDeltaPosition;

        // 특정 상태에서 중력 연산을 적용한다.
        switch (State.StateType)
        {
            case BattleUnitStateType.IDLE:
            case BattleUnitStateType.MOVE_FORWARD:
            case BattleUnitStateType.MOVE_BACK:
            {
                if (movePosition.y > Fixed64.Zero)
                {
                    movePosition += new Vector3d(Physics.gravity.x, Physics.gravity.y, Physics.gravity.z) * frame.DeltaTime;
                }
                break;
            }
        }

        // 위치가 0미만으로 떨어지지 않도록 한다.
        if (movePosition.y < Fixed64.Zero)
        {
            movePosition.y = Fixed64.Zero;
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

    public void CheckCollisions(in BattleFrame frame)
    {
        CheckAttackCollision(frame);
    }

    public void Apply(BattleWorldScene scene)
    {
        InterpolatingTime = Fixed64.Zero;
        PreviousPosition = Position;
        PreviousRotation = Rotation;

        scene.SetPositionAndRotation(ID, Position, Rotation);
        scene.SampleAnimation(ID, AnimationSampleInfo);
    }

    public void Interpolate(in BattleFrame frame, BattleWorldScene worldScene)
    {
        InterpolatingTime += frame.DeltaTime;

        var t = (InterpolatingTime / frame.FixedDeltaTime).Clamp01();
        var position = Vector3d.Lerp(PreviousPosition, Position, t);
        var rotation = FixedQuaternion.Slerp(PreviousRotation, Rotation, t);

        worldScene.SetPositionAndRotation(ID, position, rotation);

        if (AnimationSampleInfo.NextStateInfo != null &&
            AnimationSampleInfo.StateInfo is BattleUnitFiniteStateInfo finiteStateInfo &&
            AnimationSampleInfo.ElapsedTime + InterpolatingTime > finiteStateInfo.Duration)
        {
            var elasedTime = AnimationSampleInfo.ElapsedTime + InterpolatingTime - finiteStateInfo.Duration;
            AnimationSampleInfo = AnimationSampleInfo.SetNextInfo(elasedTime);
            worldScene.SampleAnimation(ID, AnimationSampleInfo);
        }
        else
        {
            worldScene.UpdateAnimation(ID, frame.DeltaTime);
        }
    }

    private FixedQuaternion LookOtherUnitRotation(in BattleFrame frame)
    {
        Fixed64 ROTATION_SNAP_THRESHOLD = new Fixed64(0.5d);
        var otherUnit = World.GetOtherUnit(ID);
        var targetRotation = FixedQuaternion.LookRotation(otherUnit.Position.ToXZ() - Position.ToXZ());
        if (FixedQuaternion.Angle(Rotation, targetRotation) < ROTATION_SNAP_THRESHOLD)
        {
            return targetRotation;
        }
        else
        {
            return FixedQuaternion.Slerp(Rotation, targetRotation, frame.DeltaTime * new Fixed64(12.0d));
        }
    }

    private Vector3d AdvanceMoveState(in BattleFrame frame)
    {
        switch (State.StateType)
        {
            case BattleUnitStateType.MOVE_BACK:
            case BattleUnitStateType.MOVE_FORWARD:
            {
                return MoveController.AdvanceTime(frame.DeltaTime, Rotation);
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

        return Vector3d.Zero;
    }

    private void CheckAttackCollision(in BattleFrame frame)
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
        var cameraTransform = World.WorldManager.Camera.FixedTransform;
        var cameraLocalPosition = cameraTransform.InverseTransformPoint(Position);

        var otherUnit = World.GetOtherUnit(ID);
        var otherUnitCameraLocalPosition = cameraTransform.InverseTransformPoint(otherUnit.Position);

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
        var cameraTransform = World.WorldManager.Camera.FixedTransform;
        var cameraLocalPosition = cameraTransform.InverseTransformPoint(Position);

        var otherUnit = World.GetOtherUnit(ID);
        var otherUnitCameraLocalPosition = cameraTransform.InverseTransformPoint(otherUnit.Position);

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
        AttackController.Initialize(new Fixed64(0.233333d), new Fixed64(0.833d), Fixed64.One, Fixed64.One, new Fixed64(1.0d), new Fixed64(0.2d));
        State.SetNextStateInfo(BattleUnitStateInfo.ATTACK1);
    }

    public void DoAttack2()
    {
        AttackController.Initialize(new Fixed64(0.4d), new Fixed64(1.0), Fixed64.One, new Fixed64(1.5d), new Fixed64(2.0d), new Fixed64(0.4d));
        State.SetNextStateInfo(BattleUnitStateInfo.ATTACK2);
    }

    public void DoHit(int damage, Fixed64 knockbackAmount, Fixed64 knockbackDuration)
    {
        HitMoveController.Initialize(Vector3d.Backward * Rotation, knockbackAmount, knockbackDuration);
        State.SetNextStateInfo(BattleUnitStateInfo.HIT);
    }

    public void PerformAttack()
    {
        var otherUnit = World.GetOtherUnit(ID);
        var attackCircle = BattleCircle.FromUnitForward(this, AttackController.AttackOffset, AttackController.AttackRange);
        var otherUnitCircle = BattleCircle.FromUnit(otherUnit);
        if (BattleCircle.CheckCollision(attackCircle, otherUnitCircle, out _))
        {
            World.PerformAttack(this, otherUnit.ID, AttackController.KnockbackAmount, AttackController.KnockbackDuration);
        }
    }

    public int GetUnitHash()
    {
        long longHash = long.MaxValue;
        longHash ^= Position.x.m_rawValue;
        longHash ^= Position.y.m_rawValue;
        longHash ^= Position.z.m_rawValue;
        longHash ^= Rotation.x.m_rawValue;
        longHash ^= Rotation.y.m_rawValue;
        longHash ^= Rotation.z.m_rawValue;
        longHash ^= Rotation.w.m_rawValue;
        var uinthash = (uint)(longHash % uint.MaxValue);
        var intHash = (int)uinthash;
        return intHash;
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
