using UnityEngine;

public struct BattleFrame
{
    public bool InFixedTimeStep { get; set; }
    public float DeltaTime { get; set; }
    public float FixedDeltaTime { get; set; }
    public float InverseFixeedDeltaTime => 1.0f / FixedDeltaTime;

    public BattleFrame(bool inFixedTimeStep, float deltaTime, float fixedDeltaTime) : this()
    {
        InFixedTimeStep = inFixedTimeStep;
        DeltaTime = deltaTime;
        FixedDeltaTime = fixedDeltaTime;
    }
}
