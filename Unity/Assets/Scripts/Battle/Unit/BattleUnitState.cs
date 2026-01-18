[ManagedState]
public partial class BattleUnitState
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    public string PreviousAnimationName { get; private set; } = string.Empty;
    public float PreviousElapsedTime { get; private set; } = 0.0f;

    public string AnimationName { get; private set; } = string.Empty;
    public float ElapsedTime = 0.0f;

    public BattleUnitState(BattleWorld world)
    {
        World = world;
    }

    public void PlayAnimation(string animationName)
    {
        PreviousAnimationName = AnimationName;
        PreviousElapsedTime = ElapsedTime;

        AnimationName = animationName;
        ElapsedTime = 0.0f;
    }

    public void AdvanceTime(float deltaTime)
    {
        ElapsedTime += deltaTime;
    }

    public BattleUnitState Clone()
    {
        var clone = World.UnitStatePool.Get();
        clone.DeepCopyFrom(this);
        return clone;
    }

    partial void OnRelease()
    {
        World.UnitStatePool.Release(this);
    }
}
