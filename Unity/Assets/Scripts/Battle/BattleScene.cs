using System.Collections;
using UnityEngine;

public class BattleScene : MonoBehaviour
{
    private BattleWorldManager WorldManager { get; set; } = new();
    private bool IsInitialized { get; set; } = false;

    private IEnumerator Start()
    {
        yield return new WaitForFixedUpdate();
        IsInitialized = true;

        WorldManager.Initialize();
    }

    private void FixedUpdate()
    {
        if (!IsInitialized) return;

        var frame = GetCurrentFrame();
        WorldManager.AdvanceFrame(frame);
    }

    private void Update()
    {
        if (!IsInitialized) return;

        var frame = GetCurrentFrame();
        WorldManager.OnUpdate(frame);
    }

    private void OnDestroy()
    {
        WorldManager.Dispose();
        WorldManager = null;
    }

    private BattleFrame GetCurrentFrame()
    {
        return new BattleFrame(Time.inFixedTimeStep, Time.deltaTime, Time.time);
    }

}
