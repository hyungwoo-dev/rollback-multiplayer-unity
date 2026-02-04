using UnityEditor.Experimental.GraphView;
using UnityEngine;

[ManagedState]
public partial class BattleUnitMoveController
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }
    public float Speed { get; private set; }
    public float ElapsedTime { get; private set; }

    public bool IsMoving => MoveSide != BattleUnitMoveSide.None;
    public BattleUnitMoveSide MoveSide { get; private set; }

    public BattleUnitMoveController(BattleWorld world)
    {
        World = world;
    }

    public void Start(BattleUnitMoveSide side, float speed)
    {
        MoveSide = side;
        Speed = speed;
    }

    public void Stop()
    {
        MoveSide = BattleUnitMoveSide.None;
        Speed = 0.0f;
    }

    public Vector3 AdvanceTime(float deltaTime, Quaternion rotation)
    {
        var directionScale = MoveSide switch
        {
            BattleUnitMoveSide.None => 0,
            BattleUnitMoveSide.Forward => 1,
            BattleUnitMoveSide.Back => -1,
        };

        var direction = rotation * Vector3.forward * directionScale;
        ElapsedTime += deltaTime;

        return direction * Speed * deltaTime;
    }

    public BattleUnitMoveController Clone()
    {
        var clone = World.UnitMoveControllerPool.Get();
        clone.DeepCopyFrom(this);
        return clone;
    }

    partial void OnRelease()
    {
        World.UnitMoveControllerPool.Release(this);
    }
}
