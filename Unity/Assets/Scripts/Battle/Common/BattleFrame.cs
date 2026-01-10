using UnityEngine;

public struct BattleFrame
{
    public bool InFixedTimeStep { get; set; }
    public float DeltaTime { get; set; }
    public float Time { get; set; }

    public BattleFrame(bool inFixedTimeStep, float deltaTime, float time) : this()
    {
        InFixedTimeStep = inFixedTimeStep;
        DeltaTime = deltaTime;
        Time = time;
    }
}
