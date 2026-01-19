using System.Collections.Generic;
using UnityEngine;

public class BattleAttackCollider : MonoBehaviour
{
    private List<int> UnitIds = new();
    private int UnitID { get; set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<BattleHitCollider>(out var hitCollider) && UnitID != hitCollider.UnitID)
        {
            UnitIds.Add(hitCollider.UnitID);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<BattleHitCollider>(out var hitCollider) && UnitID != hitCollider.UnitID)
        {
            UnitIds.Remove(hitCollider.UnitID);
        }
    }

    public void Initialize(int unitID)
    {
        UnitID = unitID;
    }

    public void GetUnitIds(List<int> unitIds)
    {
        foreach (var unitID in UnitIds)
        {
            if (unitIds.Contains(unitID))
            {
                continue;
            }

            unitIds.Add(unitID);
        }
    }
}
