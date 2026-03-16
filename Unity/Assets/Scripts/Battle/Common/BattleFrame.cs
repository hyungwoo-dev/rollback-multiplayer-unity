using FixedMathSharp;

public struct BattleFrame
{
    public bool InFixedTimeStep { get; set; }
    public Fixed64 DeltaTime { get; set; }
    public Fixed64 FixedDeltaTime { get; set; }
    public Fixed64 UnscaledDeltaTime { get; set; }

    public BattleFrame(bool inFixedTimeStep, Fixed64 deltaTime, Fixed64 fixedDeltaTime, Fixed64 unscaledDeltaTime) : this()
    {
        InFixedTimeStep = inFixedTimeStep;
        DeltaTime = deltaTime;
        FixedDeltaTime = fixedDeltaTime;
        UnscaledDeltaTime = unscaledDeltaTime;
    }
}
