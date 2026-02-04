using log4net.Util;
using UnityEngine;

public class BattleCamera : MonoBehaviour
{
    [SerializeField]
    private Vector3 CurrentVelocity;

    [SerializeField]
    private float SmoothTime = 1.0f;
    private BattleWorld World { get; set; }

    public void Initialize(BattleWorld world)
    {
        World = world;

        var (targetPosition, targetRotation) = World.GetCameraTargetPositionAndRotation(transform);
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    public void OnUpdate(in BattleFrame frame)
    {
        var (targetPosition, targetRotation) = World.GetCameraTargetPositionAndRotation(transform);

        transform.position = Vector3.Lerp(transform.position, targetPosition, frame.DeltaTime * 6.0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, frame.DeltaTime * 6.0f);
    }
}
