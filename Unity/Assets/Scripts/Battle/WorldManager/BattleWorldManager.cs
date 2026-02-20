using UnityEngine;

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
}
