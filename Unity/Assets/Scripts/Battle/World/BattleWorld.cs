using System.Collections.Generic;
using UnityEngine.Pool;

[ManagedState]
public partial class BattleWorld
{
    private Debug Debug = new(nameof(BattleWorld));

    public int ID { get; set; }
    public BattleWorldManager WorldManager { get; }
    public BattleWorldScene WorldScene { get; set; }
    public int CurrentFrame { get; private set; }
    private List<BattleWorldEventInfo> WorldEventInfos { get; set; } = new();

    public BattleWorld(BattleWorldManager worldManager)
    {
        WorldManager = worldManager;
        InitializePool();
    }

    public void Initialize(BattleWorldScene worldScene)
    {
        WorldScene = worldScene;
    }

    public void OnUpdate(in BattleFrame frame)
    {

    }

    public void OnFixedUpdate(in BattleFrame frame)
    {
        CurrentFrame += 1;
        WorldScene.SimulatePhysics(frame.DeltaTime);
    }

    public void AddWorldEventInfo(BattleWorldEventInfo worldEventInfo)
    {
        WorldEventInfos.Add(worldEventInfo);
    }

    public BattleWorld Clone()
    {
        var world = WorldManager.WorldPool.Get();
        world.DeepCopyFrom(this);
        return world;
    }

    partial void OnRelease()
    {
        WorldManager.WorldPool.Release(this);
    }

    #region Pool

    [ManagedStateIgnore]
    public ObjectPool<BattleUnit> UnitPool { get; set; }


    private void InitializePool()
    {
        UnitPool = new(() => new BattleUnit(this));
    }


    #endregion Pool
}