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

    public float NextStateElapsedTime { get; private set; }

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
        SetNextStateInfo(stateInfo, 0.0f);
    }

    public void SetNextStateInfo(BattleUnitStateInfo stateInfo, float elaspedTime)
    {
        NextStateInfo = stateInfo;
        NextStateElapsedTime = elaspedTime;
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
            ElapsedTime = NextStateElapsedTime;
        }
        else
        {
            isStateChanged = false;

            PreviousElapsedTime = ElapsedTime;
            ElapsedTime += deltaTime;

            switch (StateInfo)
            {
                case BattleUnitFiniteStateInfo finiteStateInfo:
                {
                    if (ElapsedTime > finiteStateInfo.Duration)
                    {
                        var nextStateStartTime = ElapsedTime - finiteStateInfo.Duration;
                        SetNextStateInfo(finiteStateInfo.NextStateInfo, nextStateStartTime);
                    }
                    break;
                }
                case BattleUnitLoopStateInfo loopStateInfo:
                {
                    break;
                }
            }
        }
    }

    public BattleUnitState Clone(BattleWorld context)
    {
        var clone = context.UnitStatePool.Get();
        clone.World = context;
        clone.DeepCopyFrom(context, this);
        clone.PreviousStateInfo = PreviousStateInfo;
        clone.StateInfo = StateInfo;
        clone.NextStateInfo = NextStateInfo;
        return clone;
    }

    partial void OnRelease(BattleWorld context)
    {
        context.UnitStatePool.Release(this);
    }
}
