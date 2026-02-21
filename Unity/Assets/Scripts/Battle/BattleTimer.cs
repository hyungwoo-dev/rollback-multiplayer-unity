using FixedMathSharp;

[ManagedState(typeof(BattleWorld))]
public partial class BattleTimer
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    private Fixed64 Delay { get; set; }

    public bool IsRunning() => Delay > Fixed64.Zero;

    public BattleTimer(BattleWorld world)
    {
        World = world;
    }

    public void Set(Fixed64 delay)
    {
        Delay = delay;
    }

    public bool AdvanceTime(Fixed64 deltaTime)
    {
        Delay -= deltaTime;
        if (Delay < Fixed64.Zero)
        {
            Delay = Fixed64.Zero;
            return true;
        }
        return false;
    }

    public BattleTimer Clone(BattleWorld context)
    {
        var clone = context.TimerPool.Get();
        clone.World = context;
        clone.DeepCopyFrom(context, this);
        return clone;
    }

    partial void OnRelease(BattleWorld context)
    {
        context.TimerPool.Release(this);
    }
}
