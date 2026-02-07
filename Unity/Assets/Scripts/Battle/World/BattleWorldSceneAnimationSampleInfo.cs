public struct BattleWorldSceneAnimationSampleInfo
{
    public BattleWorldSceneAnimationSampleInfo(BattleUnitState state)
    {
        PreviousAnimationName = state.PreviousStateInfo != null ? state.PreviousStateInfo.AnimationName : string.Empty;
        PreviousAnimationElapsedTime = state.PreviousStateElapsedTime;

        AnimationName = state.StateInfo.AnimationName;
        PreviousElapsedTime = state.PreviousElapsedTime;
        ElapsedTime = state.ElapsedTime;
        CrossFadeInTime = state.StateInfo.CrossFadeInTime;
    }

    public string PreviousAnimationName;
    public float PreviousAnimationElapsedTime;

    public string AnimationName;
    public float PreviousElapsedTime;
    public float ElapsedTime;

    public float CrossFadeInTime;

    public float InverseCrossFadeInTime => 1.0f / CrossFadeInTime;
}