[ManagedStateIgnore]
public class BattleWorldManager : BaseWorldManager
{
    private long StartUnixTimeMillis { get; set; }
    public override int BattleTimeMillis => (int)(TimeUtils.UtcNowUnixTimeMillis - StartUnixTimeMillis);

    public override void Setup()
    {
        base.Setup();
        StartUnixTimeMillis = TimeUtils.UtcNowUnixTimeMillis;
    }

    public override void AdvanceFrame(in BattleFrame frame)
    {
        base.AdvanceFrame(frame);

        LocalWorldEventInfos.Remove(FutureWorld.CurrentFrame, out var _);
    }
}