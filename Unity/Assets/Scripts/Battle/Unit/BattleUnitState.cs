
using System.Collections.Generic;

[ManagedState]
public partial class BattleUnitState
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    public BattleUnitStateType StateType { get; private set; } = BattleUnitStateType.IDLE;
    public string PreviousAnimationName { get; private set; } = string.Empty;
    public float PreviousAnimationElapsedTime { get; private set; } = 0.0f;

    public string AnimationName { get; private set; } = string.Empty;
    public float PreviousElapsedTime { get; private set; } = 0.0f;
    public float ElapsedTime { get; private set; } = 0.0f;

    public BattleUnitState(BattleWorld world)
    {
        World = world;
    }

    public void PlayAnimation(BattleUnitStateType stateType, string animationName)
    {
        StateType = stateType;
        SetAnimation(animationName);
    }

    private void SetAnimation(string animationName)
    {
        PreviousAnimationName = AnimationName;
        PreviousAnimationElapsedTime = ElapsedTime;

        AnimationName = animationName;
        PreviousElapsedTime = 0.0f;
        ElapsedTime = 0.0f;
    }

    public void AdvanceFrame(float deltaTime)
    {
        PreviousElapsedTime = ElapsedTime;
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
