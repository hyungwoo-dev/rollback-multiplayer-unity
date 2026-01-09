using UnityEngine;

[ManagedState]
public partial class BattleWorld
{
    private Debug Debug = new(nameof(BattleWorld));

    public int Id { get; set; }
    public BattleWorldManager WorldManager { get; }

    public BattleWorld(BattleWorldManager worldManager)
    {
        WorldManager = worldManager;
    }

    public BattleWorld New()
    {
        return WorldManager.ObjectManager.BattleWorldPool.Get();
    }

    partial void OnRelease()
    {
        WorldManager.ObjectManager.BattleWorldPool.Release(this);
    }
}
