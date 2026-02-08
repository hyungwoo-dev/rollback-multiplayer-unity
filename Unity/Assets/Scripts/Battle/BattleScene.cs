using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleScene : MonoBehaviour
{
    private static Debug Debug = new(nameof(BattleScene));

    public static void Initialize(BattleInitializationContext intializationContext)
    {
        var scene = SceneManager.LoadScene(SceneNames.BATTLE, new LoadSceneParameters()
        {
            loadSceneMode = LoadSceneMode.Single,
            localPhysicsMode = LocalPhysicsMode.Physics3D,
        });
    }

    [SerializeField]
    private BattleCamera _battleCamera;

    private BattleWorldManager WorldManager { get; set; } = new();
    private bool IsInitialized { get; set; } = false;
    private bool IsReady { get; set; } = false;

    private void Awake()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 144;
        Physics.simulationMode = SimulationMode.Script;
    }

    private IEnumerator Start()
    {
        WorldManager.Prepare();

        while (!WorldManager.IsReady())
        {
            yield return null;
        }

        yield return new WaitForFixedUpdate();

        IsReady = true;
    }

    private void Initialize()
    {
        WorldManager.Initialize(_battleCamera);
        _battleCamera.Initialize(WorldManager.FutureWorld);

        IsInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!IsReady) return;

        var frame = new BattleFrame(Time.inFixedTimeStep, Time.deltaTime, Time.fixedDeltaTime);

        if (!IsInitialized)
        {
            Initialize();
            WorldManager.FutureWorld.AdvanceFrame(frame);
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
    }

    private void OnDestroy()
    {
        WorldManager.Dispose();
        WorldManager = null;
    }
}
