using System.Collections.Generic;
using FixedMathSharp;
using UnityEngine;
using System.IO;




#if UNITY_EDITOR
using UnityEditor.Animations;
#endif // UNITY_EDITOR

public class AnimationDataGenerator : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Generate();
        }
    }

    private void Generate()
    {
        var animationDeltaInfos = new List<AnimationDeltaInfo>();
        var clipInfos = AnimatorControllerUtility.GetAllStateClipInfos(_animator);
        foreach (var clipInfo in clipInfos)
        {
            var deltaPositions = new List<Vector3d>();
            var deltaRotations = new List<FixedQuaternion>();

            var sceneAnimator = _animator.TryGetComponent<BattleWorldSceneUnitAnimator>(out var unitAnimator) ? unitAnimator : _animator.gameObject.AddComponent<BattleWorldSceneUnitAnimator>();
            sceneAnimator.PlayInFixedTime(clipInfo.StateName, clipInfo.SyncedLayerIndex, Fixed64.Zero);
            sceneAnimator.ResetDelta();

            var clipLength = new Fixed64(clipInfo.ClipLength);
            var fixedDeltaTime = new Fixed64(Time.fixedDeltaTime);
            var time = Fixed64.Zero;
            while (time < clipLength)
            {
                var nextTime = time + fixedDeltaTime > clipLength ? clipLength : time + fixedDeltaTime;
                var deltaTime = nextTime - time;
                var (deltaPosition, deltaRotation) = sceneAnimator.UpdateAnimator(deltaTime);
                deltaPositions.Add(deltaPosition);
                deltaRotations.Add(deltaRotation);

                time = nextTime;
            }

            var deltaInfo = new AnimationDeltaInfo()
            {
                StateName = clipInfo.StateName,
                IsLooping = clipInfo.IsLooping,
                DeltaPositions = deltaPositions,
                DeltaRotations = deltaRotations,
            };

            animationDeltaInfos.Add(deltaInfo);
        }

        var path = Path.Combine(Application.dataPath, "Resources", AnimationDeltaInfos.FILE_NAME);
        var deltaInfos = new AnimationDeltaInfos(animationDeltaInfos);
        var json = JsonUtility.ToJson(deltaInfos);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        Debug.Shared.Log($"애니메이션 데이터 생성. 경로: {path}");

        var readJson = File.ReadAllText(path);
        var readDeltaInfos = JsonUtility.FromJson(readJson, typeof(AnimationDeltaInfos));
    }

#endif // UNITY_EDITOR
}

#if UNITY_EDITOR

public static class AnimatorControllerUtility
{
    public struct StateClipInfo
    {
        public int SyncedLayerIndex;
        public string StateName;
        public string ClipName;
        public float ClipLength;
        public bool IsLooping;
    }

    public static List<StateClipInfo> GetAllStateClipInfos(Animator animator)
    {
        var result = new List<StateClipInfo>();

        if (animator == null)
        {
            Debug.Shared.LogError("Animator is null.");
            return result;
        }

        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            Debug.Shared.LogError("AnimatorController not found or not an AnimatorController type.");
            return result;
        }

        foreach (var layer in controller.layers)
        {
            TraverseStateMachine(layer.stateMachine, layer.syncedLayerIndex, result);
        }

        return result;
    }

    private static void TraverseStateMachine(
        AnimatorStateMachine stateMachine,
        int syncedLayerIndex,
        List<StateClipInfo> result)
    {
        // 현재 StateMachine의 상태들
        foreach (var childState in stateMachine.states)
        {
            var state = childState.state;

            if (state.motion is AnimationClip clip)
            {
                result.Add(new StateClipInfo
                {
                    SyncedLayerIndex = syncedLayerIndex,
                    StateName = state.name,
                    ClipName = clip.name,
                    ClipLength = clip.length,
                    IsLooping = clip.isLooping,
                });
            }
        }

        // 서브 스테이트 머신 재귀 순회
        foreach (var childStateMachine in stateMachine.stateMachines)
        {
            TraverseStateMachine(childStateMachine.stateMachine, syncedLayerIndex, result);
        }
    }
}
#endif
