using UnityEngine;

[ManagedState(typeof(BattleWorld))]
public partial class BattleUnitMoveController
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }
    public float Speed { get; private set; }
    public float ElapsedTime { get; private set; }

    public bool IsMoving => DirectionScale != 0;
    public int DirectionScale { get; private set; }

    public BattleUnitMoveController(BattleWorld world)
    {
        World = world;
    }

    public void Start(int directionScale, float speed)
    {
        DirectionScale = directionScale;
        Speed = speed;
    }

    public void Stop()
    {
        DirectionScale = 0;
        Speed = 0.0f;
    }

    public Vector3 AdvanceTime(float deltaTime, Quaternion rotation)
    {
        var direction = rotation * Vector3.forward * DirectionScale;
        ElapsedTime += deltaTime;

        return direction * Speed * deltaTime;
    }

    public BattleUnitMoveController Clone(BattleWorld context)
    {
        var clone = World.UnitMoveControllerPool.Get();
        clone.World = context;
        clone.DeepCopyFrom(context, this);
        return clone;
    }

    partial void OnRelease(BattleWorld context)
    {
        context.UnitMoveControllerPool.Release(this);
    }
}
