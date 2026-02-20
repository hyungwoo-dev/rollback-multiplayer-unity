[ManagedState(typeof(BaseWorldManager))]
public partial class BattleWorldEventInfo
{
    private BaseWorldManager WorldManager { get; set; }

    public int TargetFrame { get; set; }
    public int UnitID { get; set; }
    public BattleWorldInputEventType WorldInputEventType { get; set; }
    public int BattleTimeMillis { get; set; }

    public BattleWorldEventInfo(BaseWorldManager worldManager)
    {
        WorldManager = worldManager;
    }

    public BattleWorldEventInfo Clone(BaseWorldManager context)
    {
        var clone = WorldManager.WorldEventInfoPool.Get();
        clone.WorldManager = context;
        clone.DeepCopyFrom(context, this);
        return clone;
    }

    partial void OnRelease(BaseWorldManager context)
    {
        context.WorldEventInfoPool.Release(this);
    }

    public override int GetHashCode()
    {
        return (TargetFrame << 16) | UnitID << 8 | (int)WorldInputEventType;
    }
}
