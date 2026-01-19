using UnityEngine;
using UnityEngine.Pool;

[ManagedState]
public partial class BattleUnit
{
    private const float DASH_MOVE_AMOUNT = 1.0f;
    private const float DASH_MOVE_TIME = 0.25f;

    private const float HIT_MOVE_AMOUNT = 0.5f;
    private const float HIT_MOVE_TIME = 1.167f;

    [ManagedStateIgnore]
    public BattleWorld World { get; set; }
    public int ID { get; private set; }
    public Vector3 Position { get; private set; }

    private BattleWorldSceneObjectHandle Handle { get; set; }
    private BattleUnitState State { get; set; }
    private BattleUnitMoveController DashMoveController { get; set; }
    private BattleUnitJumpController JumpController { get; set; }
    private BattleUnitAttackController AttackController { get; set; }
    private BattleUnitMoveController HitMoveController { get; set; }

    public bool CanDash() => State.StateType == BattleUnitStateType.IDLE;
    public bool CanAttack() => State.StateType == BattleUnitStateType.IDLE;
    public bool CanJump() => State.StateType == BattleUnitStateType.IDLE;

    public BattleUnit(BattleWorld world)
    {
        World = world;
        State = world.UnitStatePool.Get();
        DashMoveController = world.UnitMoveControllerPool.Get();
        JumpController = world.UnitJumpControllerPool.Get();
        AttackController = world.UnitAttackControllerPool.Get();
        HitMoveController = world.UnitMoveControllerPool.Get();
    }

    public void Initialize(int unitID, Vector3 position, Quaternion rotation)
    {
        ID = unitID;
        Position = position;
        Handle = World.WorldScene.Instantiate(BattleWorldResources.UNIT, position, rotation);
        var sceneUnit = World.WorldScene.GetSceneUnit(Handle);
        sceneUnit.Initialize(unitID);

        State.PlayAnimation(BattleUnitStateType.IDLE, BattleUnitAnimationNames.IDLE);
    }

    public void OnFixedUpdate(in BattleFrame frame)
    {
        var moveDelta = Vector3.zero;
        UpdateState(frame, ref moveDelta);

        if (moveDelta != Vector3.zero)
        {
            Position += moveDelta;
        }

        World.WorldScene.SetPosition(Handle, Position);
        World.WorldScene.SampleAnimation(Handle, BattleWorldSceneAnimationSampleInfo.From(State));

        State.AdvanceTime(frame.DeltaTime);
    }

    private void UpdateState(BattleFrame frame, ref Vector3 moveDelta)
    {
        switch (State.StateType)
        {
            case BattleUnitStateType.IDLE:
            {
                break;
            }
            case BattleUnitStateType.DASH:
            {
                if (DashMoveController.IsMoving())
                {
                    moveDelta += DashMoveController.AdvanceTime(frame.DeltaTime);
                }
                else
                {
                    State.PlayAnimation(BattleUnitStateType.IDLE, BattleUnitAnimationNames.IDLE);
                }
                break;
            }
            case BattleUnitStateType.JUMPING:
            {
                if (JumpController.IsJumping())
                {
                    moveDelta += JumpController.AdvanceTime(frame);
                }
                else
                {
                    State.PlayAnimation(BattleUnitStateType.IDLE, BattleUnitAnimationNames.IDLE);
                }
                break;
            }
            case BattleUnitStateType.ATTACK:
            {
                if (AttackController.IsRunning())
                {
                    AttackController.AdvanceTime(frame, out var performAttack);
                    if (performAttack)
                    {
                        PerformAttack();
                    }
                }
                else
                {
                    State.PlayAnimation(BattleUnitStateType.IDLE, BattleUnitAnimationNames.IDLE);
                }
                break;
            }
            case BattleUnitStateType.HIT:
            {
                if (HitMoveController.IsMoving())
                {
                    moveDelta += HitMoveController.AdvanceTime(frame.DeltaTime);
                }
                else
                {
                    State.PlayAnimation(BattleUnitStateType.IDLE, BattleUnitAnimationNames.IDLE);
                }
                break;
            }
        }
    }

    public void OnUpdate(in BattleFrame frame)
    {
        World.WorldScene.UpdateAnimation(Handle, frame.DeltaTime);
    }

    public void DoLeftDash()
    {
        State.PlayAnimation(BattleUnitStateType.DASH, BattleUnitAnimationNames.IDLE);
        DashMoveController.Initialize(Vector3.left, DASH_MOVE_AMOUNT, DASH_MOVE_TIME);
    }

    public void DoRightDash()
    {
        State.PlayAnimation(BattleUnitStateType.DASH, BattleUnitAnimationNames.IDLE);
        DashMoveController.Initialize(Vector3.right, DASH_MOVE_AMOUNT, DASH_MOVE_TIME);
    }

    public void DoAttack1()
    {
        AttackController.Initialize(0.333333f, 1.0f);
        State.PlayAnimation(BattleUnitStateType.ATTACK, BattleUnitAnimationNames.ATTACK1);
    }

    public void DoAttack2()
    {
        AttackController.Initialize(0.5f, 1.029f);
        State.PlayAnimation(BattleUnitStateType.ATTACK, BattleUnitAnimationNames.ATTACK2);
    }

    public void DoFire()
    {
        State.PlayAnimation(BattleUnitStateType.ATTACK, BattleUnitAnimationNames.FIRE);
    }

    public void DoHit(int damage)
    {
        HitMoveController.Initialize(Vector3.right, HIT_MOVE_AMOUNT, HIT_MOVE_TIME);
        State.ForcePlayAnimation(BattleUnitStateType.HIT, BattleUnitAnimationNames.HIT);
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
