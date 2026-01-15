using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[ManagedStateIgnore]
public class BattleWorldScene
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

    public void Initialize()
    {
        var scene = LoadScene();
        PhysicsScene = scene.GetPhysicsScene();
        RootGameObject = scene.GetRootGameObjects().First();
    }

    public void SimulatePhysics(float step)
    {
        PhysicsScene.Simulate(step);
    }

    public BattleWorldSceneObjectHandle Instantiate(BattleWorldResource worldResource)
    {
        switch (WorldSceneKind)
        {
            case BattleWorldSceneKind.GRAPHICS:
            {
                return Instantiate(worldResource.ResourcePath);
            }
            case BattleWorldSceneKind.NO_GRAPHICS:
            {
                return Instantiate(worldResource.NoGraphicsResourcePath);
            }
            default:
            {
                throw new NotSupportedException($"Instantiate NotSupported {nameof(WorldSceneKind)}: {WorldSceneKind}");
            }
        }
    }

    public BattleWorldSceneObjectHandle Instantiate(string resourcePath)
    {
        var asset = Resources.Load<GameObject>(resourcePath);
        var gameObject = GameObject.Instantiate(asset, RootGameObject.transform);
        var gameObjectID = GenerateGameObjectID();
        GameObjectDictionary.Add(gameObjectID, gameObject);
        return new BattleWorldSceneObjectHandle(gameObjectID);
    }

    public void SampleAnimation(BattleWorldSceneObjectHandle handle, string stateName, float fixedTime)
    {
        if (GameObjectDictionary.TryGetValue(handle.ID, out var gameObject))
        {
            var animator = gameObject.GetComponent<Animator>();
            if (animator != null)
            {
                animator.PlayInFixedTime(stateName, 0, fixedTime);
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
    }

    public void UpdateAnimation(BattleWorldSceneObjectHandle handle, float deltaTime)
    {
        if (GameObjectDictionary.TryGetValue(handle.ID, out var gameObject))
        {
            var animator = gameObject.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Update(deltaTime);
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
