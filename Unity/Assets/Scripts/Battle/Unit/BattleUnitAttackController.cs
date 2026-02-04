using UnityEngine;

[ManagedState]
public partial class BattleUnitAttackController
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    private float PerformTiming { get; set; }
    private float Duration { get; set; }
    private float ElapsedTime { get; set; }

    public BattleUnitAttackController(BattleWorld world)
    {
        World = world;
    }

    public bool IsAttacking()
    {
        return ElapsedTime < Duration;
    }

    public void Initialize(float performTiming, float duration)
    {
        ElapsedTime = 0.0f;
        PerformTiming = performTiming;
        Duration = duration;
    }

    public void AdvanceTime(in BattleFrame frame, out bool performAttack)
    {
        var nextElapsedTime = Mathf.Min(ElapsedTime + frame.DeltaTime, Duration);
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

    public BattleUnitAttackController Clone()
    {
        var clone = World.UnitAttackControllerPool.Get();
        clone.DeepCopyFrom(this);
        return clone;
    }

    partial void OnRelease()
    {
        World.UnitAttackControllerPool.Release(this);
    }
}
