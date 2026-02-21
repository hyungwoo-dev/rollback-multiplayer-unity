using FixedMathSharp;

[ManagedState(typeof(BattleWorld))]
public partial class BattleUnitAttackController
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    private Fixed64 PerformTiming { get; set; }
    private Fixed64 Duration { get; set; }
    private Fixed64 ElapsedTime { get; set; }

    public BattleUnitAttackController(BattleWorld world)
    {
        World = world;
    }

    public bool IsAttacking()
    {
        return ElapsedTime < Duration;
    }

    public void Initialize(Fixed64 performTiming, Fixed64 duration)
    {
        ElapsedTime = Fixed64.Zero;
        PerformTiming = performTiming;
        Duration = duration;
    }

    public void AdvanceTime(in BattleFrame frame, out bool performAttack)
    {
        var nextElapsedTime = MathUtils.Min(ElapsedTime + frame.DeltaTime, Duration);
        if (PerformTiming >= ElapsedTime && PerformTiming < nextElapsedTime)
        {
            performAttack = true;
        }
        else
        {
            performAttack = false;
        }
        ElapsedTime = nextElapsedTime;
    }

    public BattleUnitAttackController Clone(BattleWorld context)
    {
        var clone = context.UnitAttackControllerPool.Get();
        clone.World = context;
        clone.DeepCopyFrom(context, this);
        return clone;
    }

    partial void OnRelease(BattleWorld context)
    {
        context.UnitAttackControllerPool.Release(this);
    }
}
