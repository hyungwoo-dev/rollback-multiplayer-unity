using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[ManagedState]
public partial class BattleUnitMoveController
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    public Vector3 Direction { get; private set; }
    public float Speed { get; private set; }
    public float ElapsedTime { get; private set; }

    public bool IsMoving => MoveSide != BattleUnitMoveSide.None;
    public BattleUnitMoveSide MoveSide { get; private set; }

    public BattleUnitMoveController(BattleWorld world)
    {
        World = world;
    }

    public void Start(BattleUnitMoveSide side, Vector3 direction, float speed)
    {
        MoveSide = side;
        Direction = direction;
        Speed = speed;
    }

    public void Stop()
    {
        MoveSide = BattleUnitMoveSide.None;
        Direction = Vector3.zero;
        Speed = 0.0f;
    }

    public Vector3 AdvanceTime(float deltaTime)
    {
        ElapsedTime += deltaTime;

        return Direction * Speed * deltaTime;
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
