using UnityEngine;

public class BattleHitCollider : MonoBehaviour
{
    public int UnitID { get; private set; } = 0;

    public void Initialize(int unitID)
    {
        UnitID = unitID;
    }
}
