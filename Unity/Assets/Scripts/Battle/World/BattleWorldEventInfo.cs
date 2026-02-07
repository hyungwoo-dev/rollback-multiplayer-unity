[ManagedState(typeof(BattleWorldManager))]
public partial class BattleWorldEventInfo
{
    private BattleWorldManager WorldManager { get; set; }

    public int TargetFrame { get; set; }
    public int UnitID { get; set; }
    public BattleWorldInputEventType WorldInputEventType { get; set; }

    public BattleWorldEventInfo(BattleWorldManager worldManager)
    {
        WorldManager = worldManager;
    }

    public BattleWorldEventInfo Clone(BattleWorldManager context)
    {
        var clone = WorldManager.WorldEventInfoPool.Get();
        clone.WorldManager = context;
        clone.DeepCopyFrom(context, this);
        return clone;
    }

    partial void OnRelease(BattleWorldManager context)
    {
        context.WorldEventInfoPool.Release(this);
    }

    public override int GetHashCode()
    {
        return (TargetFrame << 16) | UnitID << 8 | (int)WorldInputEventType;
    }
}
