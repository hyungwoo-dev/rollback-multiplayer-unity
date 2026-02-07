using Codice.CM.Client.Differences;

[ManagedState(typeof(BattleWorld))]
public partial class BattleTimer
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    private float Delay { get; set; }

    public bool IsRunning() => Delay > 0.0f;

    public BattleTimer(BattleWorld world)
    {
        World = world;
    }

    public void Set(float delay)
    {
        Delay = delay;
    }

    public bool AdvanceTime(float deltaTime)
    {
        Delay -= deltaTime;
        if (Delay < 0)
        {
            Delay = 0.0f;
            return true;
        }
        return false;
    }

    public BattleTimer Clone()
    {
        var clone = World.TimerPool.Get();
        clone.DeepCopyFrom(this);
        return clone;
    }

    partial void OnRelease()
    {
        World.TimerPool.Release(this);
    }
}
