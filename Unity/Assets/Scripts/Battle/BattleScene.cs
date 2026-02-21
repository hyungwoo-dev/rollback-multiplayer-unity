using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleScene : MonoBehaviour
{
    private static Debug Debug = new(nameof(BattleScene));

    public static IEnumerator CoLoad(BattleSceneLoadContext context)
    {
        var hertz = Mathf.CeilToInt((float)Screen.currentResolution.refreshRateRatio.value);
        Application.targetFrameRate = hertz;
        Physics.simulationMode = SimulationMode.Script;
        Debug.Log($"디스플레이 하드웨어 주사율: {hertz}");

        var scene = SceneManager.LoadScene(SceneNames.BATTLE, new LoadSceneParameters()
        {
            loadSceneMode = LoadSceneMode.Additive,
            localPhysicsMode = LocalPhysicsMode.Physics3D,
        });

        while (!scene.isLoaded)
        {
            yield return null;
        }

        foreach (var gameObject in scene.GetRootGameObjects())
        {
            if (!gameObject.TryGetComponent<BattleScene>(out var battleScene))
            {
                continue;
            }

            battleScene.Setup(context.PlayMode);
            break;
        }
    }

    [SerializeField]
    private BattleCamera _battleCamera;

    private BaseWorldManager WorldManager { get; set; }
    private bool IsInitialized { get; set; } = false;
    private bool IsSetup { get; set; } = false;

    public void Setup(BattlePlayMode playMode)
    {
        WorldManager = playMode switch
        {
            BattlePlayMode.Singleplay => new BattleWorldManager(),
            BattlePlayMode.Multiplay => new MultiplayBattleWorldManager(),
            _ => throw new NotImplementedException()
        };

        StartCoroutine(CoSetup());
    }

    private IEnumerator CoSetup()
    {
        WorldManager.Setup();

        while (!WorldManager.IsSetupCompleted())
        {
            yield return null;
        }

        IsSetup = true;
        WorldManager.OnSetupCompleted();
    }

    private void Initialize()
    {
        WorldManager.Initialize(_battleCamera);
        _battleCamera.Initialize(WorldManager.FutureWorld);

        IsInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!IsSetup) return;

        var frame = new BattleFrame(Time.inFixedTimeStep, Time.deltaTime, Time.fixedDeltaTime);

        if (!IsInitialized)
        {
            Initialize();
        }

        if (WorldManager.IsStarted())
        {
            WorldManager.AdvanceFrame(frame);
            _battleCamera.OnFixedUpdate(frame);
        }
    }

    private void Update()
    {
        if (!IsInitialized) return;
        if (!WorldManager.IsStarted()) return;

        var deltaTime = Mathf.Min(Time.deltaTime, Time.time - Time.fixedTime);
        var frame = new BattleFrame(Time.inFixedTimeStep, deltaTime, Time.fixedDeltaTime);
        WorldManager.OnUpdate(frame);
        _battleCamera.Interpolate(frame);

        // TODO: 
        if (Input.GetKeyDown(KeyCode.Return))
        {
            var fixedFrame = new BattleFrame(true, Time.fixedDeltaTime, Time.fixedDeltaTime);
            StartCoroutine(WorldManager.CoSelfResimulate(fixedFrame));
        }
    }

    private void OnDestroy()
    {
        if (WorldManager != null)
        {
            WorldManager.Dispose();
            WorldManager = null;
        }
    }
}
