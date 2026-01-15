using UnityEngine;
[ManagedState]
public partial class BattleUnit
{
    [ManagedStateIgnore]
    public BattleWorld World { get; set; }

    public BattleUnit(BattleWorld world)
    {
        World = world;
    }
}
