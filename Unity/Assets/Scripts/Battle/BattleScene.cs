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
    private BattleCamera BattleCamera;

    private BattleWorldManager WorldManager { get; set; } = new();
    private bool IsInitialized { get; set; } = false;
    private bool IsReady { get; set; } = false;

    private void Awake()
    {
        Application.runInBackground = true;
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
        WorldManager.Initialize();
        BattleCamera.Initialize(WorldManager.LocalWorld);

        IsInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!IsReady) return;

        if (!IsInitialized)
        {
            Initialize();
        }

        var frame = new BattleFrame(Time.inFixedTimeStep, Time.deltaTime, Time.fixedDeltaTime, Time.time);
        WorldManager.OnFixedUpdate(frame);
    }

    private void Update()
    {
        if (!IsInitialized) return;

        var deltaTime = Mathf.Min(Time.deltaTime, Time.time - Time.fixedTime);
        var frame = new BattleFrame(Time.inFixedTimeStep, deltaTime, Time.fixedDeltaTime, Time.time);
        WorldManager.OnUpdate(frame);
        BattleCamera.OnUpdate(frame);
    }

    private void OnDestroy()
    {
        WorldManager.Dispose();
        WorldManager = null;
    }
}
