using FixedMathSharp;

public abstract class BattleUnitStateInfo
{
    public static readonly Fixed64 CROSS_FADE_IN_TIME_ZERO = new Fixed64(0.0d);
    public static readonly Fixed64 CROSS_FADE_IN_TIME = new Fixed64(0.1d);

    public static readonly BattleUnitStateInfo IDLE = new BattleUnitLoopStateInfo(BattleUnitStateType.IDLE, BattleUnitAnimationNames.IDLE, CROSS_FADE_IN_TIME);
    public static readonly BattleUnitStateInfo MOVE_BACK = new BattleUnitLoopStateInfo(BattleUnitStateType.MOVE_BACK, BattleUnitAnimationNames.MOVE_BACK, CROSS_FADE_IN_TIME);
    public static readonly BattleUnitStateInfo MOVE_FORWARD = new BattleUnitLoopStateInfo(BattleUnitStateType.MOVE_FORWARD, BattleUnitAnimationNames.MOVE_FORWARD, CROSS_FADE_IN_TIME);

    public static readonly BattleUnitStateInfo ATTACK1 = new BattleUnitFiniteStateInfo(BattleUnitStateType.ATTACK, BattleUnitAnimationNames.ATTACK1, CROSS_FADE_IN_TIME, new Fixed64(0.833d), BattleUnitStateInfo.IDLE);
    public static readonly BattleUnitStateInfo ATTACK2 = new BattleUnitFiniteStateInfo(BattleUnitStateType.ATTACK, BattleUnitAnimationNames.ATTACK2, CROSS_FADE_IN_TIME, new Fixed64(1.0d), BattleUnitStateInfo.IDLE);

    public static readonly BattleUnitStateInfo HIT = new BattleUnitFiniteStateInfo(BattleUnitStateType.HIT, BattleUnitAnimationNames.HIT, CROSS_FADE_IN_TIME_ZERO, new Fixed64(0.53333d), BattleUnitStateInfo.IDLE);

    public abstract BattleUnitStateType StateType { get; }
    public abstract string AnimationName { get; }
    public abstract Fixed64 CrossFadeInTime { get; }
}

public class BattleUnitLoopStateInfo : BattleUnitStateInfo
{
    public override BattleUnitStateType StateType { get; }
    public override string AnimationName { get; }
    public override Fixed64 CrossFadeInTime { get; }

    public BattleUnitLoopStateInfo(BattleUnitStateType stateType, string animationName, Fixed64 crossFadingTime)
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
    public override Fixed64 CrossFadeInTime { get; }

    public Fixed64 Duration { get; }
    public BattleUnitStateInfo NextStateInfo { get; }

    public BattleUnitFiniteStateInfo(BattleUnitStateType stateType, string animationName, Fixed64 crossFadingTime, Fixed64 duration, BattleUnitStateInfo nextStateInfo)
    {
        StateType = stateType;
        AnimationName = animationName;
        CrossFadeInTime = crossFadingTime;
        Duration = duration;
        NextStateInfo = nextStateInfo;
    }
}