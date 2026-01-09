using System.Resources;
using UnityEngine;
using UnityEngine.Pool;

public class BattleObjectManager
{
    public ObjectPool<BattleWorld> BattleWorldPool { get; }

    public BattleObjectManager(BattleWorldManager worldManager)
    {
        BattleWorldPool = new ObjectPool<BattleWorld>(createFunc: () => new BattleWorld(worldManager), defaultCapacity: 2);
    }

    public void Dispose()
    {
        BattleWorldPool.Dispose();
    }
}
