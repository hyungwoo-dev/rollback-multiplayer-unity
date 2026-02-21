using FixedMathSharp;

[ManagedState(typeof(BattleWorld))]
public partial class BattleUnitState
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    public Fixed64 PreviousStateElapsedTime { get; private set; } = Fixed64.Zero;

    public Fixed64 PreviousElapsedTime { get; private set; } = Fixed64.Zero;
    public Fixed64 ElapsedTime { get; private set; } = Fixed64.Zero;

    [ManagedStateIgnore]
    public BattleUnitStateInfo PreviousStateInfo { get; private set; }

    [ManagedStateIgnore]
    public BattleUnitStateInfo StateInfo { get; private set; }

    [ManagedStateIgnore]
    public BattleUnitStateInfo NextStateInfo { get; private set; }

    public Fixed64 NextStateElapsedTime { get; private set; } = Fixed64.Zero;

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
        SetNextStateInfo(stateInfo, Fixed64.Zero);
    }

    public void SetNextStateInfo(BattleUnitStateInfo stateInfo, Fixed64 elaspedTime)
    {
        NextStateInfo = stateInfo;
        NextStateElapsedTime = elaspedTime;
    }

    public void AdvanceFrame(Fixed64 deltaTime, out bool isStateChanged)
    {
        if (NextStateInfo != null)
        {
            isStateChanged = true;

            PreviousStateElapsedTime = ElapsedTime;
            PreviousStateInfo = StateInfo;
            StateInfo = NextStateInfo;
            NextStateInfo = null;

            PreviousElapsedTime = Fixed64.Zero;
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
