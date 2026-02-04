using Codice.CM.Common.Tree.Partial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public BattleWorldScene(BattleWorldManager worldManager, BattleWorldSceneKind worldSceneKind)
    {
        WorldManager = worldManager;
        WorldSceneKind = worldSceneKind;
    }

    public void Prepare()
    {
        Scene = LoadScene();
        PhysicsScene = Scene.GetPhysicsScene();
    }

    public void Initialize()
    {
        RootGameObject = Scene.GetRootGameObjects().First();
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
            case BattleWorldSceneKind.GRAPHICS:
            {
                return Instantiate(worldResource.ResourcePath, position, rotation);
            }
            case BattleWorldSceneKind.NO_GRAPHICS:
            {
                return Instantiate(worldResource.NoGraphicsResourcePath, position, rotation);
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
        var gameObjectID = GenerateGameObjectID();
        GameObjectDictionary.Add(gameObjectID, gameObject);
        return new BattleWorldSceneObjectHandle(gameObjectID);
    }

    public void SetPositionAndRotation(BattleWorldSceneObjectHandle handle, Vector3 position, Quaternion rotation)
    {
        if (GameObjectDictionary.TryGetValue(handle.ID, out var gameObject))
        {
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = rotation;
        }
        else
        {
            Debug.LogError($"{nameof(SetPositionAndRotation)} Not Found GameObject ID: {handle.ID}");
        }
    }

    public void ApplyDeltaPositionAndRotation(BattleWorldSceneObjectHandle handle, Vector3 deltaPosition, Quaternion deltaRotation)
    {
        if (GameObjectDictionary.TryGetValue(handle.ID, out var gameObject))
        {
            gameObject.transform.localPosition += deltaPosition;
            gameObject.transform.localRotation *= deltaRotation;
        }
        else
        {
            Debug.LogError($"{nameof(SetPositionAndRotation)} Not Found GameObject ID: {handle.ID}");
        }
    }

    public (Vector3 DeltaPosition, Quaternion DeltaRotation) SampleAnimation(BattleWorldSceneObjectHandle handle, BattleWorldSceneAnimationSampleInfo sampleInfo)
    {
        if (GameObjectDictionary.TryGetValue(handle.ID, out var gameObject))
        {
            var animator = gameObject.GetComponentInChildren<BattleWorldSceneUnitAnimator>();
            if (animator != null)
            {
                var isCrossFading = !string.IsNullOrWhiteSpace(sampleInfo.PreviousAnimationName) &&
                                    sampleInfo.PreviousAnimationName != sampleInfo.AnimationName &&
                                    sampleInfo.ElapsedTime < BattleWorldSceneAnimationSampleInfo.CROSS_FADE_TIME;
                if (isCrossFading)
                {
                    animator.PlayInFixedTime(
                        animationName: sampleInfo.PreviousAnimationName,
                        animationLayer: 0,
                        fixedTime: sampleInfo.PreviousAnimationElapsedTime);

                    animator.CrossFadeInFixedTime(
                        animationName: sampleInfo.AnimationName,
                        fixedTransitionDuration: BattleWorldSceneAnimationSampleInfo.CROSS_FADE_TIME,
                        animationLayer: 0,
                        fixedTimeOffset: sampleInfo.PreviousElapsedTime,
                        normalizedTransitionTime: sampleInfo.PreviousElapsedTime * BattleWorldSceneAnimationSampleInfo.INVERSE_CROSS_FADE_TIME);

                    animator.ResetDelta();

                    var deltaTime = sampleInfo.ElapsedTime == 0 ? 0 : sampleInfo.ElapsedTime - sampleInfo.PreviousElapsedTime;
                    var result = animator.OnUpdate(deltaTime);
                    return result;
                }
                else
                {
                    animator.PlayInFixedTime(sampleInfo.AnimationName, 0, sampleInfo.PreviousElapsedTime);
                    animator.ResetDelta();

                    var deltaTime = sampleInfo.ElapsedTime == 0 ? 0 : sampleInfo.ElapsedTime - sampleInfo.PreviousElapsedTime;
                    var result = animator.OnUpdate(deltaTime);
                    return result;
                }
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
                return animator.OnUpdate(deltaTime);
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
            case BattleWorldSceneKind.GRAPHICS:
            {
                return SceneManager.LoadScene(SceneNames.BATTLE_WORLD_GRAPHICS, new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            }
            case BattleWorldSceneKind.NO_GRAPHICS:
            {
                return SceneManager.LoadScene(SceneNames.BATTLE_WORLD_NO_GRAPHICS, new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            }
            default:
            {
                throw new NotSupportedException($"LoadScene NotSupported {nameof(WorldSceneKind)}: {WorldSceneKind}");
            }
        }
    }
}
