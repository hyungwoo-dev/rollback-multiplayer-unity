[ManagedState]
public partial class BattleWorldEventInfo
{
    private BattleWorldManager WorldManager { get; }

    public int TargetFrame { get; set; }
    public int UnitID { get; set; }
    public BattleWorldInputEventType WorldInputEventType { get; set; }

    public BattleWorldEventInfo(BattleWorldManager worldManager)
    {
        WorldManager = worldManager;
    }

    public BattleWorldEventInfo Clone()
    {
        var clone = WorldManager.WorldEventInfoPool.Get();
        clone.DeepCopyFrom(this);
        return clone;
    }

    partial void OnRelease()
    {
        WorldManager.WorldEventInfoPool.Release(this);
    }

    public override int GetHashCode()
    {
        return (TargetFrame << 16) | UnitID << 8 | (int)WorldInputEventType;
    }
}
