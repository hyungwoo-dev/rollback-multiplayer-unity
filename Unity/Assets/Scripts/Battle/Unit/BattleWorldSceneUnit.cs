using UnityEngine;

public class BattleWorldSceneUnit : MonoBehaviour
{
    [SerializeField]
    private BattleAttackCollider[] AttackColliders;

    private void Awake()
    {
        AttackColliders = GetComponentsInChildren<BattleAttackCollider>(true);
    }

    public void SetWorldScene(BattleWorldScene worldScene)
    {

    }

    public void SetID(int id)
    {

    }
}
