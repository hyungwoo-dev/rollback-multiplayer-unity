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
    }

    public void OnUpdate(in BattleFrame frame)
    {
        //var rect = World.GetRectContainingUnits();

        //var targetPosition = transform.position;
        //targetPosition.x = rect.center.x;
        //targetPosition.y = rect.center.y;

        //var newPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref CurrentVelocity, SmoothTime);
        //transform.position = newPosition;

        var (targetPosition, targetRotation) = World.GetCameraTargetPositionAndRotation(transform);

        transform.position = Vector3.Lerp(transform.position, targetPosition, frame.DeltaTime * 6.0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, frame.DeltaTime * 6.0f);
    }
}
