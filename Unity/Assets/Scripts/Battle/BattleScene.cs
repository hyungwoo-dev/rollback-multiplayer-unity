using FixedMathSharp;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleScene : MonoBehaviour
{
    private static Debug Debug = new(nameof(BattleScene));

    public static IEnumerator CoLoad(BattleSceneLoadContext context)
    {
        AnimationDeltaInfos.EnsureInstance();

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

    private void Initialize(in BattleFrame frame)
    {
        WorldManager.Initialize(frame);
        _battleCamera.Initialize(WorldManager.FutureWorld.CameraTransform);
        IsInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!IsSetup) return;

        var frame = new BattleFrame(Time.inFixedTimeStep, new Fixed64(Time.deltaTime), new Fixed64(Time.fixedDeltaTime));

        if (!IsInitialized)
        {
            Initialize(frame);
        }

        if (WorldManager.IsStarted())
        {
            WorldManager.AdvanceFrame(frame);
        }
    }

    private void Update()
    {
        if (!IsSetup) return;
        if (!IsInitialized) return;
        if (!WorldManager.IsStarted()) return;

        var deltaTime = Mathf.Min(Time.deltaTime, Time.time - Time.fixedTime);
        var frame = new BattleFrame(Time.inFixedTimeStep, new Fixed64(deltaTime), new Fixed64(Time.fixedDeltaTime));

        WorldManager.OnUpdate(frame);
        _battleCamera.OnUpdate(WorldManager.FutureWorld.CameraTransform, frame);
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
