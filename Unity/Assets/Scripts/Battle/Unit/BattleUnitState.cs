[ManagedState(typeof(BattleWorld))]
public partial class BattleUnitState
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    public float PreviousStateElapsedTime { get; private set; } = 0.0f;

    public float PreviousElapsedTime { get; private set; } = 0.0f;
    public float ElapsedTime { get; private set; } = 0.0f;

    [ManagedStateIgnore]
    public BattleUnitStateInfo PreviousStateInfo { get; private set; }

    [ManagedStateIgnore]
    public BattleUnitStateInfo StateInfo { get; private set; }

    [ManagedStateIgnore]
    public BattleUnitStateInfo NextStateInfo { get; private set; }

    public BattleUnitStateType StateType => StateInfo.StateType;

    public BattleUnitState(BattleWorld world)
    {
        World = world;
    }

    public void SetStateInfo(BattleUnitStateInfo stateInfo)
    {
        StateInfo = stateInfo;
    }

    public void SetNextStateInfo(BattleUnitStateInfo stateInfo)
    {
        NextStateInfo = stateInfo;
    }

    public void AdvanceFrame(float deltaTime, out bool isStateChanged)
    {
        if (NextStateInfo != null)
        {
            isStateChanged = true;

            PreviousStateElapsedTime = ElapsedTime;
            PreviousStateInfo = StateInfo;
            StateInfo = NextStateInfo;
            NextStateInfo = null;

            PreviousElapsedTime = 0.0f;
            ElapsedTime = 0.0f;
        }
        else
        {
            isStateChanged = false;

            PreviousElapsedTime = ElapsedTime;
            ElapsedTime += deltaTime;
        }
    }

    public BattleUnitState Clone()
    {
        var clone = World.UnitStatePool.Get();
        clone.DeepCopyFrom(this);
        clone.PreviousStateInfo = PreviousStateInfo;
        clone.StateInfo = StateInfo;
        clone.NextStateInfo = NextStateInfo;
        return clone;
    }

    partial void OnRelease()
    {
        World.UnitStatePool.Release(this);
    }
}
