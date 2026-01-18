using UnityEngine;

[ManagedState]
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
        if (ElapsedTime + deltaTime > MoveTime)
        {
            deltaTime = MoveTime - ElapsedTime;
            ElapsedTime = MoveTime;
        }
        else
        {
            ElapsedTime += deltaTime;
        }

        // 일정 시간동안 일정 거리만큼 특정 방향으로 이동한다.
        // 속도는 중력가속도 법칙에 의해 줄어든다.
        var moveDelta = (Velocity - (Deceleration * deltaTime * 0.5f)) * deltaTime;
        Velocity -= Deceleration * deltaTime;
        return moveDelta * Vector3.up;
    }

    public bool IsFinished()
    {
        return ElapsedTime == MoveTime;
    }

    public BattleUnitJumpMove Clone()
    {
        var clone = World.UnitJumpMovePool.Get();
        clone.DeepCopyFrom(this);
        return clone;
    }

    partial void OnRelease()
    {
        World.UnitJumpMovePool.Release(this);
    }
}