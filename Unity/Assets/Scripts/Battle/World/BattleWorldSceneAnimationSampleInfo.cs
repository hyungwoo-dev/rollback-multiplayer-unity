public struct BattleWorldSceneAnimationSampleInfo
{
    public static readonly float CROSS_FADE_TIME = 0.1f;
    public static readonly float INVERSE_CROSS_FADE_TIME = 1.0f / CROSS_FADE_TIME;

    public static BattleWorldSceneAnimationSampleInfo From(BattleUnitState state)
    {
        return new BattleWorldSceneAnimationSampleInfo()
        {
            PreviousAnimationName = state.PreviousAnimationName,
            PreviousElapsedTime = state.PreviousElapsedTime,
            AnimationName = state.AnimationName,
            ElapsedTime = state.ElapsedTime,
        };
    }

    public string PreviousAnimationName;
    public float PreviousElapsedTime;

    public string AnimationName;
    public float ElapsedTime;
}