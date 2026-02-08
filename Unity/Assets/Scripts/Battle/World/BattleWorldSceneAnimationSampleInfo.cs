public struct BattleWorldSceneAnimationSampleInfo
{
    public string PreviousAnimationName;
    public float PreviousAnimationElapsedTime;

    public string AnimationName;
    public float PreviousElapsedTime;
    public float ElapsedTime;

    public float CrossFadeInTime;

    public BattleUnitStateInfo StateInfo;
    public BattleUnitStateInfo NextStateInfo;

    public float InverseCrossFadeInTime => 1.0f / CrossFadeInTime;

    public BattleWorldSceneAnimationSampleInfo(BattleUnitState state)
    {
        PreviousAnimationName = state.PreviousStateInfo != null ? state.PreviousStateInfo.AnimationName : string.Empty;
        PreviousAnimationElapsedTime = state.PreviousStateElapsedTime;

        AnimationName = state.StateInfo.AnimationName;
        PreviousElapsedTime = state.PreviousElapsedTime;
        ElapsedTime = state.ElapsedTime;
        CrossFadeInTime = state.StateInfo.CrossFadeInTime;

        StateInfo = state.StateInfo;
        NextStateInfo = state.NextStateInfo;
    }

    public BattleWorldSceneAnimationSampleInfo SetNextInfo(float elapsedTime)
    {
        return new BattleWorldSceneAnimationSampleInfo()
        {
            PreviousAnimationName = StateInfo.AnimationName,
            PreviousAnimationElapsedTime = ElapsedTime,
            AnimationName = NextStateInfo.AnimationName,
            PreviousElapsedTime = 0.0f,
            ElapsedTime = elapsedTime,
            CrossFadeInTime = NextStateInfo.CrossFadeInTime,
            StateInfo = NextStateInfo,
            NextStateInfo = null,
        };
    }
}