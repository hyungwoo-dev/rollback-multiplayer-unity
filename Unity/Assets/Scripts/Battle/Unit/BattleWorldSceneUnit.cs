using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BattleWorldSceneUnit : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private BattleAttackCollider[] AttackColliders;

    [SerializeField]
    private BattleHitCollider[] HitColliders;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        AttackColliders = GetComponentsInChildren<BattleAttackCollider>(true);
        HitColliders = GetComponentsInChildren<BattleHitCollider>(true);
    }

    public void Initialize(int unitID)
    {
        foreach (var attackCollider in AttackColliders)
        {
            attackCollider.Initialize(unitID);
        }

        foreach (var hitCollider in HitColliders)
        {
            hitCollider.Initialize(unitID);
        }
    }

    public void GetUnitIds(List<int> unitIds)
    {
        foreach (var attackCollider in AttackColliders)
        {
            attackCollider.GetUnitIds(unitIds);
        }
    }

    private void OnAnimatorMove()
    {
        if(_animator.deltaPosition.sqrMagnitude > 0)
        {
            Debug.Shared.Log($"DeltaPosition: {_animator.deltaPosition}");
        }
    }
}
