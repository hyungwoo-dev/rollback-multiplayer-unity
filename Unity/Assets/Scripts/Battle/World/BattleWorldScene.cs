using FixedMathSharp;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct BattleCircle
{
    public Vector3d Position { get; private set; }
    public Fixed64 Radius { get; private set; }

    public BattleCircle(Vector3d position, Fixed64 radius) : this()
    {
        Position = position;
        Radius = radius;
    }

    public static BattleCircle FromUnitForward(BattleUnit unit, Fixed64 forwardDistance, Fixed64 radius)
    {
        var forward = unit.Rotation * Vector3d.Forward;
        var position = unit.Position + forward * forwardDistance;
        return new BattleCircle(position, radius);
    }

    public static BattleCircle FromUnit(BattleUnit unit)
    {
        return new BattleCircle(unit.Position, Fixed64.Half);
    }

    public static bool CheckCollision(BattleCircle a, BattleCircle b, out Fixed64 overlapDistance)
    {
        var sumRadius = (a.Radius + b.Radius);
        var distance = (a.Position - b.Position).Magnitude;

        if (distance > sumRadius)
        {
            overlapDistance = Fixed64.Zero;
            return false;
        }

        overlapDistance = sumRadius - distance;
        return true;
    }
}

[ManagedStateIgnore]
public partial class BattleWorldScene
{
    private static Debug Debug = new(nameof(BattleWorldScene));

    private BaseWorldManager WorldManager { get; }
    private Scene Scene { get; set; }
    private PhysicsScene PhysicsScene { get; set; }
    private GameObject RootGameObject { get; set; }
    private int Layer { get; set; }

    public BattleWorldScene(BaseWorldManager worldManager, int layer)
    {
        WorldManager = worldManager;
        Layer = layer;
    }

    public void Load()
    {
        Scene = LoadScene();
        PhysicsScene = Scene.GetPhysicsScene();
    }

    public void Dispose()
    {
        SceneManager.UnloadSceneAsync(Scene);
    }

    public void Initialize()
    {
        RootGameObject = Scene.GetRootGameObjects().First(gameObject => gameObject.name == "Root");
    }

    public bool IsSceneLoaded()
    {
        return Scene.isLoaded;
    }

    private Dictionary<int, GameObject> _units = new();

    public void InstantiateUnit(int unitID, Vector3d position, FixedQuaternion rotation)
    {
        var gameObject = Instantiate("AndroidUnit/Prefabs/AndroidUnit", position, rotation);
        _units.Add(unitID, gameObject);
    }

    public GameObject Instantiate(string resourcePath)
    {
        return Instantiate(resourcePath, Vector3d.Zero, FixedQuaternion.Identity);
    }

    public GameObject Instantiate(string resourcePath, Vector3d position, FixedQuaternion rotation)
    {
        var asset = Resources.Load<GameObject>(resourcePath);
        var gameObject = GameObject.Instantiate(asset, position.ToVector3(), rotation.ToQuaternion(), RootGameObject.transform);
        return gameObject;
    }

    private void SetGameObjectLayerRecursively(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform)
        {
            SetGameObjectLayerRecursively(child.gameObject, layer);
        }
    }

    public void SetPositionAndRotation(int unitID, Vector3d position, FixedQuaternion rotation)
    {
        if (_units.TryGetValue(unitID, out var gameObject))
        {
            gameObject.transform.position = position.ToVector3();
            gameObject.transform.rotation = rotation.ToQuaternion();
        }
    }

    public (Vector3d deltaPosition, FixedQuaternion deltaRotation) SampleAnimation(int unitID, in BattleWorldSceneAnimationSampleInfo sampleInfo)
    {
        if (_units.TryGetValue(unitID, out var gameObject))
        {
            var animator = gameObject.GetComponentInChildren<BattleWorldSceneUnitAnimator>();
            if (animator != null)
            {
                var isCrossFading = !string.IsNullOrWhiteSpace(sampleInfo.PreviousAnimationName) &&
                                    sampleInfo.PreviousAnimationName != sampleInfo.AnimationName &&
                                    sampleInfo.ElapsedTime < sampleInfo.CrossFadeInTime;
                if (isCrossFading)
                {
                    animator.PlayInFixedTime(
                        animationName: sampleInfo.PreviousAnimationName,
                        animationLayer: 0,
                        fixedTime: sampleInfo.PreviousAnimationElapsedTime);

                    animator.CrossFadeInFixedTime(
                        animationName: sampleInfo.AnimationName,
                        fixedTransitionDuration: sampleInfo.CrossFadeInTime,
                        animationLayer: 0,
                        fixedTimeOffset: sampleInfo.PreviousElapsedTime,
                        normalizedTransitionTime: sampleInfo.PreviousElapsedTime / sampleInfo.CrossFadeInTime);

                    animator.ResetDelta();
                }
                else
                {
                    animator.PlayInFixedTime(sampleInfo.AnimationName, 0, sampleInfo.PreviousElapsedTime);
                    animator.ResetDelta();
                }

                var deltaTime = sampleInfo.ElapsedTime - sampleInfo.PreviousElapsedTime;
                return animator.UpdateAnimator(deltaTime);
            }
            else
            {
                Debug.LogError($"{nameof(SampleAnimation)} Have Not Animator. Name: {gameObject.name}, UnitID: {unitID}");
            }
        }
        else
        {
            Debug.LogError($"{nameof(SampleAnimation)} Not Found UnitID: {unitID}");
        }

        return (Vector3d.Zero, FixedQuaternion.Identity);
    }

    public (Vector3d DeltaPosition, FixedQuaternion DeltaRotation) UpdateAnimation(int unitID, Fixed64 deltaTime)
    {
        if (_units.TryGetValue(unitID, out var gameObject))
        {
            var animator = gameObject.GetComponentInChildren<BattleWorldSceneUnitAnimator>();
            if (animator != null)
            {
                return animator.UpdateAnimator(deltaTime);
            }
            else
            {
                Debug.LogError($"{nameof(UpdateAnimation)} Have Not Animator. Name: {gameObject.name}, UnitID: {unitID}");
            }
        }
        else
        {
            Debug.LogError($"{nameof(UpdateAnimation)} Not Found GameObject. UnitID: {unitID}");
        }

        return (Vector3d.Zero, FixedQuaternion.Identity);
    }

    private Scene LoadScene()
    {
        return SceneManager.LoadScene(SceneNames.BATTLE_WORLD, new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.None));
    }
}
