using NUnit.Framework;
using UnityEngine;
using static BattleWorldScene;

[ManagedState]
public partial class BattleUnit
{
    private const float DASH_MOVE_AMOUNT = 1.0f;
    private const float DASH_MOVE_TIME = 0.25f;

    [ManagedStateIgnore]
    public BattleWorld World { get; set; }
    public int ID { get; private set; }
    private BattleWorldSceneObjectHandle Handle { get; set; }
    private BattleUnitState State { get; set; }

    private BattleUnitMove DashMove { get; set; }

    private BattleUnitJumpController JumpController { get; set; }

    public bool CanDash() => DashMove.IsFinished();
    public bool CanAttack() => true;
    public bool CanJump() => JumpController.CanJump();

    public BattleUnit(BattleWorld world)
    {
        World = world;
        State = world.UnitStatePool.Get();
        DashMove = world.UnitMovePool.Get();
        JumpController = world.UnitJumpControllerPool.Get();
    }

    public void Initialize()
    {
        Handle = World.WorldScene.Instantiate(BattleWorldResources.UNIT);
        State.PlayAnimation(BattleUnitAnimationName.IDLE);
    }

    public void OnFixedUpdate(in BattleFrame frame)
    {
        var moveDelta = DashMove.AdvanceTime(frame.DeltaTime);
        if (JumpController.IsRunning())
        {
            var (jumpMoveDelta, isJumpFinished) = JumpController.AdvanceTime(frame);
            moveDelta += jumpMoveDelta;

            if (isJumpFinished)
            {
                State.PlayAnimation(BattleUnitAnimationName.IDLE);
            }
        }

        World.WorldScene.SampleAnimation(Handle, BattleWorldSceneAnimationSampleInfo.From(State));
        World.WorldScene.Move(Handle, moveDelta);
        State.AdvanceTime(frame.DeltaTime);
    }

    public void OnUpdate(in BattleFrame frame)
    {
        World.WorldScene.UpdateAnimation(Handle, frame.DeltaTime);
    }

    public void DoLeftDash()
    {
        DashMove.Initialize(Vector3.left, DASH_MOVE_AMOUNT, DASH_MOVE_TIME);
    }

    public void DoRightDash()
    {
        DashMove.Initialize(Vector3.right, DASH_MOVE_AMOUNT, DASH_MOVE_TIME);
    }

    public void DoAttack1()
    {

    }

    public void DoAttack2()
    {

    }

    public void DoFire()
    {

    }

    public void DoJump()
    {
        JumpController.DoJump();
        State.PlayAnimation(BattleUnitAnimationName.JUMP);
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
