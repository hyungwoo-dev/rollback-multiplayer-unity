using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleWorldSceneUnitAnimator : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;
    public Vector3 DeltaPosition { get; private set; }
    public Quaternion DeltaRotation { get; private set; }

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnAnimatorMove()
    {
        DeltaPosition += _animator.deltaPosition;
        DeltaRotation *= _animator.deltaRotation;
    }

    public void PlayInFixedTime(string animationName, int animationLayer, float fixedTime)
    {
        _animator.PlayInFixedTime(animationName, animationLayer, fixedTime);
    }

    public void CrossFadeInFixedTime(string animationName, float fixedTransitionDuration, int animationLayer, float fixedTimeOffset, float normalizedTransitionTime)
    {
        _animator.CrossFadeInFixedTime(animationName, fixedTransitionDuration, animationLayer, fixedTimeOffset, normalizedTransitionTime);
    }

    public void ResetDelta()
    {
        _animator.Update(0.0f);
        DeltaPosition = Vector3.zero;
        DeltaRotation = Quaternion.identity;
    }

    public (Vector3 DeltaPosition, Quaternion DeltaRotation) UpdateAnimator(float deltaTime)
    {
        _animator.Update(deltaTime);

        var result = (DeltaPosition, DeltaRotation);
        DeltaPosition = Vector3.zero;
        DeltaRotation = Quaternion.identity;
        return result;
    }
}
