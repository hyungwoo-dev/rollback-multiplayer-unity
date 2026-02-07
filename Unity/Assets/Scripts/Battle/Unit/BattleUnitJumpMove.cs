using UnityEngine;

[ManagedState(typeof(BattleWorld))]
public partial class BattleUnitJumpMove
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    public float Velocity { get; private set; }
    public float Deceleration { get; private set; }
    public float MoveTime { get; private set; }
    public float ElapsedTime { get; private set; }

    public BattleUnitJumpMove(BattleWorld world)
    {
        World = world;
    }

    public void Initialize(float distance, float time)
    {
        var riseTime = time * 0.5f;
        Velocity = distance * (1.0f / riseTime) * 2;
        Deceleration = Velocity / riseTime;
        MoveTime = time;
        ElapsedTime = 0.0f;
    }

    public Vector3 AdvanceTime(float deltaTime)
    {
        if (ElapsedTime + deltaTime >= MoveTime)
        {
            deltaTime = MoveTime - ElapsedTime;
            var moveDelta = GetMovedAmount(ElapsedTime + deltaTime) - GetMovedAmount(ElapsedTime);
            ElapsedTime = MoveTime;
            return moveDelta * Vector3.up;
        }
        else
        {
            var moveDelta = GetMovedAmount(ElapsedTime + deltaTime) - GetMovedAmount(ElapsedTime);
            ElapsedTime += deltaTime;
            return moveDelta * Vector3.up;
        }
    }

    private float GetMovedAmount(float time)
    {
        return (Velocity * time) - (Deceleration * 0.5f * time * time);
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