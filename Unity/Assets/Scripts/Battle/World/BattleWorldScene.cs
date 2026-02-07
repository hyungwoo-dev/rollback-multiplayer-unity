using Codice.CM.Common.Tree.Partial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static UnityEditor.FilePathAttribute;

[ManagedStateIgnore]
public partial class BattleWorldScene
{
    private static Debug Debug = new(nameof(BattleWorldScene));

    private BattleWorldManager WorldManager { get; }
    private BattleWorldSceneKind WorldSceneKind { get; }
    private Scene Scene { get; set; }
    private PhysicsScene PhysicsScene { get; set; }
    private GameObject RootGameObject { get; set; }

    private Dictionary<int, GameObject> GameObjectDictionary { get; set; } = new();
    private int CurrentGameObjectID { get; set; } = 0;
    private int Layer { get; set; }

    public BattleWorldScene(BattleWorldManager worldManager, BattleWorldSceneKind worldSceneKind, int layer)
    {
        WorldManager = worldManager;
        WorldSceneKind = worldSceneKind;
        Layer = layer;
    }

    public void Prepare()
    {
        Scene = LoadScene();
        PhysicsScene = Scene.GetPhysicsScene();
    }

    public void Initialize()
    {
        RootGameObject = Scene.GetRootGameObjects().First(gameObject => gameObject.name == "Root");
    }

    public bool IsReady()
    {
        return Scene.isLoaded;
    }

    public void SimulatePhysics(float step)
    {
        PhysicsScene.Simulate(step);
    }

    public BattleWorldSceneObjectHandle Instantiate(BattleWorldResource worldResource, Vector3 position, Quaternion rotation)
    {
        switch (WorldSceneKind)
        {
            case BattleWorldSceneKind.VIEW:
            {
                return Instantiate(worldResource.ViewResourcePath, position, rotation);
            }
            case BattleWorldSceneKind.PLAYING:
            {
                return Instantiate(worldResource.ResourcePath, position, rotation);
            }
            default:
            {
                throw new NotSupportedException($"Instantiate NotSupported {nameof(WorldSceneKind)}: {WorldSceneKind}");
            }
        }
    }

    public BattleWorldSceneUnit GetSceneUnit(BattleWorldSceneObjectHandle handle)
    {
        if (GameObjectDictionary.TryGetValue(handle.ID, out var gameObject) && gameObject.TryGetComponent<BattleWorldSceneUnit>(out var sceneUnit))
        {
            return sceneUnit;
        }

        Debug.LogError($"Not found scene unit. ID: {handle.ID}");
        return null;
    }

    public BattleWorldSceneObjectHandle Instantiate(string resourcePath)
    {
        return Instantiate(resourcePath, Vector3.zero, Quaternion.identity);
    }

    public BattleWorldSceneObjectHandle Instantiate(string resourcePath, Vector3 position, Quaternion rotation)
    {
        var asset = Resources.Load<GameObject>(resourcePath);
        var gameObject = GameObject.Instantiate(asset, position, rotation, RootGameObject.transform);
        SetGameObjectLayerRecursively(gameObject, Layer);
        var gameObjectID = GenerateGameObjectID();
        GameObjectDictionary.Add(gameObjectID, gameObject);
        return new BattleWorldSceneObjectHandle(gameObjectID);
    }

    private void SetGameObjectLayerRecursively(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform)
        {
            SetGameObjectLayerRecursively(child.gameObject, layer);
        }
    }

    private bool TryGetComponent<T>(BattleWorldSceneObjectHandle handle, out T component) where T : Component
    {
        if (GameObjectDictionary.TryGetValue(handle.ID, out var gameObject))
        {
            if (gameObject.TryGetComponent<T>(out component))
            {
                return true;
            }
            else
            {
                Debug.LogError($"{nameof(TryGetComponent)} Not Found Component, GameObject ID: {handle.ID}");
            }
        }
        else
        {
            Debug.LogError($"{nameof(TryGetComponent)} Not Found GameObject ID: {handle.ID}");
        }

        component = null;
        return false;
    }

    public bool TryGetPosition(BattleWorldSceneObjectHandle handle, out Vector3 position)
    {
        if (TryGetComponent<Transform>(handle, out var transform))
        {
            position = transform.position;
            return true;
        }
        else
        {
            position = Vector3.zero;
            return false;
        }
    }

    public void SetPositionAndRotation(BattleWorldSceneObjectHandle handle, Vector3 position, Quaternion rotation)
    {
        if (TryGetComponent<Transform>(handle, out var transform))
        {
            transform.SetPositionAndRotation(position, rotation);
        }
    }

    public void MovePosition(BattleWorldSceneObjectHandle handle, Vector3 position)
    {
        if (TryGetComponent<Rigidbody>(handle, out var rigidbody))
        {
            rigidbody.MovePosition(position);
        }
    }

    public (Vector3 deltaPosition, Quaternion deltaRotation) SampleAnimation(BattleWorldSceneObjectHandle handle, in BattleWorldSceneAnimationSampleInfo sampleInfo)
    {
        if (GameObjectDictionary.TryGetValue(handle.ID, out var gameObject))
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
                        normalizedTransitionTime: sampleInfo.PreviousElapsedTime * sampleInfo.InverseCrossFadeInTime);

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
                Debug.LogError($"{nameof(SampleAnimation)} Have Not Animator. Name: {gameObject.name}, GameObjectID: {handle.ID}");
            }
        }
        else
        {
            Debug.LogError($"{nameof(SampleAnimation)} Not Found GameObject ID: {handle.ID}");
        }

        return (Vector3.zero, Quaternion.identity);
    }

    public (Vector3 DeltaPosition, Quaternion DeltaRotation) UpdateAnimation(BattleWorldSceneObjectHandle handle, float deltaTime)
    {
        if (GameObjectDictionary.TryGetValue(handle.ID, out var gameObject))
        {
            var animator = gameObject.GetComponentInChildren<BattleWorldSceneUnitAnimator>();
            if (animator != null)
            {
                return animator.UpdateAnimator(deltaTime);
            }
            else
            {
                Debug.LogError($"{nameof(UpdateAnimation)} Have Not Animator. Name: {gameObject.name}, GameObjectID: {handle.ID}");
            }
        }
        else
        {
            Debug.LogError($"{nameof(UpdateAnimation)} Not Found GameObject. ID: {handle.ID}");
        }

        return (Vector3.zero, Quaternion.identity);
    }

    private int GenerateGameObjectID()
    {
        return CurrentGameObjectID++;
    }

    private Scene LoadScene()
    {
        switch (WorldSceneKind)
        {
            case BattleWorldSceneKind.VIEW:
            {
                return SceneManager.LoadScene(SceneNames.BATTLE_WORLD_VIEW, new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.None));
            }
            case BattleWorldSceneKind.PLAYING:
            {
                return SceneManager.LoadScene(SceneNames.BATTLE_WORLD, new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            }
            default:
            {
                throw new NotSupportedException($"LoadScene NotSupported {nameof(WorldSceneKind)}: {WorldSceneKind}");
            }
        }
    }
}
