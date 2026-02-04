public struct BattleWorldSceneAnimationSampleInfo
{
    public static readonly float CROSS_FADE_TIME = 0.1f;
    public static readonly float INVERSE_CROSS_FADE_TIME = 1.0f / CROSS_FADE_TIME;

    public BattleWorldSceneAnimationSampleInfo(BattleUnitState state)
    {
        PreviousAnimationName = state.PreviousAnimationName;
        PreviousAnimationElapsedTime = state.PreviousAnimationElapsedTime;

        AnimationName = state.AnimationName;
        PreviousElapsedTime = state.PreviousElapsedTime;
        ElapsedTime = state.ElapsedTime;
    }

    public string PreviousAnimationName;
    public float PreviousAnimationElapsedTime;

    public string AnimationName;
    public float PreviousElapsedTime;
    public float ElapsedTime;
}