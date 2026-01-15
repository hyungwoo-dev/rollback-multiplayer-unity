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

	private BattleWorldManager WorldManager { get; set; } = new();
	private bool IsInitialized { get; set; }
	private Coroutine LateFixedUpdateCoroutine { get; set; }

	private IEnumerator Start()
	{
		while (!WorldManager.IsReady())
		{
			yield return null;
		}

		yield return new WaitForFixedUpdate();

		Initialize();
	}

	private void Initialize()
	{
		Debug.Log($"Initialize FixedTime: {Time.fixedTime}, Time: {Time.time}");
		WorldManager.Initialize();
		IsInitialized = true;
	}

	private void FixedUpdate()
	{
		if (!IsInitialized) return;

		var frame = GetCurrentFrame();
		WorldManager.OnFixedUpdate(frame);
		Debug.Log($"FixedUpdate FixedTime: {Time.fixedTime}");
	}

	private void Update()
	{
		if (!IsInitialized) return;

		var frame = GetCurrentFrame();
		WorldManager.OnUpdate(frame);
		Debug.Log($"Update Time: {Time.time}");
	}

	private void OnDestroy()
	{
		WorldManager.Dispose();
		WorldManager = null;

		if (IsInitialized)
		{
			StopCoroutine(LateFixedUpdateCoroutine);
			LateFixedUpdateCoroutine = null;
		}
	}

	private BattleFrame GetCurrentFrame()
	{
		return new BattleFrame(Time.inFixedTimeStep, Time.deltaTime, Time.time);
	}
}
