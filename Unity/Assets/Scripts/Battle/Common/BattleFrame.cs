using UnityEngine;

public struct BattleFrame
{
    public bool InFixedTimeStep { get; set; }
    public float DeltaTime { get; set; }
    public float FixedDeltaTime { get; set; }
    public float Time { get; set; }

    public BattleFrame(bool inFixedTimeStep, float deltaTime, float fixedDeltaTime, float time) : this()
    {
        InFixedTimeStep = inFixedTimeStep;
        DeltaTime = deltaTime;
        FixedDeltaTime = fixedDeltaTime;
        Time = time;
    }
}
