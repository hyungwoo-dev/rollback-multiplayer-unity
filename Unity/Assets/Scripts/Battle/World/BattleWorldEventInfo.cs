[ManagedState]
public partial class BattleWorldEventInfo
{
    private BattleWorldManager WorldManager { get; }

    public int TargetFrame { get; set; }
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
}
