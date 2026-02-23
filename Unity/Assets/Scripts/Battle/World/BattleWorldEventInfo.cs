[ManagedState(typeof(BattleWorld))]
public partial class BattleWorldEventInfo
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    public int TargetFrame { get; set; }
    public int UnitID { get; set; }
    public BattleWorldInputEventType WorldInputEventType { get; set; }
    public int BattleTimeMillis { get; set; }

    public BattleWorldEventInfo(BattleWorld world)
    {
        World = world;
    }

    public BattleWorldEventInfo Clone(BattleWorld context)
    {
        var clone = World.WorldEventInfoPool.Get();
        clone.World = context;
        clone.DeepCopyFrom(context, this);
        return clone;
    }

    partial void OnRelease(BattleWorld context)
    {
        context.WorldEventInfoPool.Release(this);
    }
}
