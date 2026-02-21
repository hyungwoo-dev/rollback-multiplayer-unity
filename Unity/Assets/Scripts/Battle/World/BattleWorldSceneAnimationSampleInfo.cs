using FixedMathSharp;

public struct BattleWorldSceneAnimationSampleInfo
{
    public string PreviousAnimationName;
    public Fixed64 PreviousAnimationElapsedTime;

    public string AnimationName;
    public Fixed64 PreviousElapsedTime;
    public Fixed64 ElapsedTime;
    public int ElapsedFrame;

    public Fixed64 CrossFadeInTime;

    public BattleUnitStateInfo StateInfo;
    public BattleUnitStateInfo NextStateInfo;

    public BattleWorldSceneAnimationSampleInfo(BattleUnitState state)
    {
        PreviousAnimationName = state.PreviousStateInfo != null ? state.PreviousStateInfo.AnimationName : string.Empty;
        PreviousAnimationElapsedTime = state.PreviousStateElapsedTime;

        AnimationName = state.StateInfo.AnimationName;
        PreviousElapsedTime = state.PreviousElapsedTime;
        ElapsedTime = state.ElapsedTime;
        ElapsedFrame = state.ElapsedFrame;

        CrossFadeInTime = state.StateInfo.CrossFadeInTime;

        StateInfo = state.StateInfo;
        NextStateInfo = state.NextStateInfo;
    }

    public BattleWorldSceneAnimationSampleInfo SetNextInfo(Fixed64 elapsedTime)
    {
        return new BattleWorldSceneAnimationSampleInfo()
        {
            PreviousAnimationName = StateInfo.AnimationName,
            PreviousAnimationElapsedTime = ElapsedTime,
            AnimationName = NextStateInfo.AnimationName,
            PreviousElapsedTime = Fixed64.Zero,
            ElapsedTime = elapsedTime,
            CrossFadeInTime = NextStateInfo.CrossFadeInTime,
            StateInfo = NextStateInfo,
            NextStateInfo = null,
        };
    }
}