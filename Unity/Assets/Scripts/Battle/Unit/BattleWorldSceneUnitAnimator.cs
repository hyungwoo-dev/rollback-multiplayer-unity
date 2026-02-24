using FixedMathSharp;
using UnityEngine;

public class BattleWorldSceneUnitAnimator : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;
    public Vector3d DeltaPosition { get; private set; }
    public FixedQuaternion DeltaRotation { get; private set; }

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnAnimatorMove()
    {
        DeltaPosition += _animator.deltaPosition.ToVector3d();
        DeltaRotation *= _animator.deltaRotation.ToFixedQuaternion();
    }

    public void PlayInFixedTime(string animationName, int animationLayer, Fixed64 fixedTime)
    {
        _animator.PlayInFixedTime(animationName, animationLayer, fixedTime.ToPreciseFloat());
    }

    public void CrossFadeInFixedTime(string animationName, Fixed64 fixedTransitionDuration, int animationLayer, Fixed64 fixedTimeOffset, Fixed64 normalizedTransitionTime)
    {
        _animator.CrossFadeInFixedTime(animationName, fixedTransitionDuration.ToPreciseFloat(), animationLayer, fixedTimeOffset.ToPreciseFloat(), normalizedTransitionTime.ToPreciseFloat());
    }

    public void ResetDelta()
    {
        _animator.Update(0.0f);
        DeltaPosition = Vector3d.Zero;
        DeltaRotation = FixedQuaternion.Identity;
    }

    public (Vector3d DeltaPosition, FixedQuaternion DeltaRotation) UpdateAnimator(Fixed64 deltaTime)
    {
        var deltaTimeF = deltaTime.ToPreciseFloat();
        _animator.Update(deltaTimeF);

        var result = (DeltaPosition, DeltaRotation);
        DeltaPosition = Vector3d.Zero;
        DeltaRotation = FixedQuaternion.Identity;
        return result;
    }
}
