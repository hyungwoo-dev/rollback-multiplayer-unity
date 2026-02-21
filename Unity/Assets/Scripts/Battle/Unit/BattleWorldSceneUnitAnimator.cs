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
        //if (Application.targetFrameRate > 0)
        //{
        //    do
        //    {
        //        var targetFrameDeltaTime = 1.0f / (float)Application.targetFrameRate;
        //        var updateDeltaTime = deltaTime > targetFrameDeltaTime ? targetFrameDeltaTime : deltaTime;
        //        _animator.Update(updateDeltaTime);
        //        deltaTime = Mathf.Max(deltaTime - updateDeltaTime, 0.0f);
        //    }
        //    while (deltaTime > 0.0f);
        //}
        //else
        {
            _animator.Update(deltaTime);
        }

        var result = (DeltaPosition, DeltaRotation);
        DeltaPosition = Vector3.zero;
        DeltaRotation = Quaternion.identity;
        return result;
    }
}
