using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[ManagedState]
public partial class BattleUnitMove
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }
    public Vector3 Direction { get; private set; }

    public float Velocity { get; private set; }
    public float Deceleration { get; private set; }
    public float MoveTime { get; private set; }
    public float ElapsedTime { get; private set; }

    public BattleUnitMove(BattleWorld world)
    {
        World = world;
    }

    public void Initialize(Vector3 direction, float distance, float time)
    {
        Direction = direction;
        Velocity = distance * (1.0f / time) * 2;
        Deceleration = Velocity / time;
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
        return moveDelta * Direction;
    }

    public bool IsFinished()
    {
        return ElapsedTime == MoveTime;
    }

    public BattleUnitMove Clone()
    {
        var clone = World.UnitMovePool.Get();
        clone.DeepCopyFrom(this);
        return clone;
    }

    partial void OnRelease()
    {
        World.UnitMovePool.Release(this);
    }
}
