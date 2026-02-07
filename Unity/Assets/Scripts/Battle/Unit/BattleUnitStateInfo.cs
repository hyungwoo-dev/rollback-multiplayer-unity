
public abstract class BattleUnitStateInfo
{
    public static readonly float CROSS_FADE_IN_TIME_ZERO = 0.0f;
    public static readonly float CROSS_FADE_IN_TIME = 0.1f;

    public static readonly BattleUnitStateInfo IDLE = new BattleUnitLoopStateInfo(BattleUnitStateType.IDLE, BattleUnitAnimationNames.IDLE, CROSS_FADE_IN_TIME);
    public static readonly BattleUnitStateInfo MOVE_BACK = new BattleUnitLoopStateInfo(BattleUnitStateType.MOVE_BACK, BattleUnitAnimationNames.MOVE_BACK, CROSS_FADE_IN_TIME);
    public static readonly BattleUnitStateInfo MOVE_FORWARD = new BattleUnitLoopStateInfo(BattleUnitStateType.MOVE_FORWARD, BattleUnitAnimationNames.MOVE_FORWARD, CROSS_FADE_IN_TIME);

    public static readonly BattleUnitStateInfo ATTACK1 = new BattleUnitFiniteStateInfo(BattleUnitStateType.ATTACK, BattleUnitAnimationNames.ATTACK1, CROSS_FADE_IN_TIME, 0.833f, BattleUnitStateInfo.IDLE);
    public static readonly BattleUnitStateInfo ATTACK2 = new BattleUnitFiniteStateInfo(BattleUnitStateType.ATTACK, BattleUnitAnimationNames.ATTACK2, CROSS_FADE_IN_TIME, 1.0f, BattleUnitStateInfo.IDLE);

    public static readonly BattleUnitStateInfo JUMP = new BattleUnitFiniteStateInfo(BattleUnitStateType.JUMPING, BattleUnitAnimationNames.JUMP, CROSS_FADE_IN_TIME, 1.0f, BattleUnitStateInfo.IDLE);

    public static readonly BattleUnitStateInfo HIT = new BattleUnitFiniteStateInfo(BattleUnitStateType.HIT, BattleUnitAnimationNames.HIT, CROSS_FADE_IN_TIME_ZERO, 0.533333f, BattleUnitStateInfo.IDLE);

    public abstract BattleUnitStateType StateType { get; }
    public abstract string AnimationName { get; }
    public abstract float CrossFadeInTime { get; }
    public float InverseCrossFadingTime => 1.0f / CrossFadeInTime;
}

public class BattleUnitLoopStateInfo : BattleUnitStateInfo
{
    public override BattleUnitStateType StateType { get; }
    public override string AnimationName { get; }
    public override float CrossFadeInTime { get; }

    public BattleUnitLoopStateInfo(BattleUnitStateType stateType, string animationName, float crossFadingTime)
    {
        StateType = stateType;
        AnimationName = animationName;
        CrossFadeInTime = crossFadingTime;
    }
}

public class BattleUnitFiniteStateInfo : BattleUnitStateInfo
{
    public override BattleUnitStateType StateType { get; }
    public override string AnimationName { get; }
    public override float CrossFadeInTime { get; }

    public float Duration { get; }
    public BattleUnitStateInfo NextStateInfo { get; }

    public BattleUnitFiniteStateInfo(BattleUnitStateType stateType, string animationName, float crossFadingTime, float duration, BattleUnitStateInfo nextStateInfo)
    {
        StateType = stateType;
        AnimationName = animationName;
        CrossFadeInTime = crossFadingTime;
        Duration = duration;
        NextStateInfo = nextStateInfo;
    }
}