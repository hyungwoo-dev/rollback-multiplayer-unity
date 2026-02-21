using FixedMathSharp;

[ManagedState(typeof(BattleWorld))]
public partial class BattleUnitMoveController
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }
    public Fixed64 Speed { get; private set; }
    public Fixed64 ElapsedTime { get; private set; }

    public bool IsMoving => DirectionScale != 0;
    public int DirectionScale { get; private set; }

    public BattleUnitMoveController(BattleWorld world)
    {
        World = world;
    }

    public void Start(int directionScale, Fixed64 speed)
    {
        DirectionScale = directionScale;
        Speed = speed;
    }

    public void Stop()
    {
        DirectionScale = 0;
        Speed = Fixed64.Zero;
    }

    public Vector3d AdvanceTime(Fixed64 deltaTime, FixedQuaternion rotation)
    {
        var direction = rotation * Vector3d.Forward * DirectionScale;
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
