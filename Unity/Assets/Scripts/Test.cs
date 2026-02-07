using UnityEngine;

public class Test : MonoBehaviour
{
    public BattleWorldSceneUnitAnimator Animator;
    public string AnimationName = "H2H_LeftCutKick_Forward";
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Animator.PlayInFixedTime(AnimationName, 0, 0.0f);
            Animator.ResetDelta();
            for (var i = 0; i < 10; ++i)
            {
                Animator.ManualUpdate(0.1f);
            }
            transform.position += Animator.DeltaPosition;
            transform.rotation *= Animator.DeltaRotation;
            Log();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Animator.PlayInFixedTime(AnimationName, 0, 0.0f);
            Animator.ResetDelta();
            Animator.ManualUpdate(1.0f);
            transform.position += Animator.DeltaPosition;
            transform.rotation *= Animator.DeltaRotation;
            Log();
        }
    }

    private void Log()
    {
        Debug.Shared.Log($"DeltaPosition: {Animator.DeltaPosition}, DeltaRotation: {Animator.DeltaRotation}");
    }
}
