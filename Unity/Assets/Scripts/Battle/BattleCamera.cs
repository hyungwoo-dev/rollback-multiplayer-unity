using UnityEngine;


public class BattleCamera : MonoBehaviour
{
    private const float POSITION_SMOOTH_TIME = 0.15f;
    private const float MAX_ROTATION_DEGREES = 90.0f;

    private Vector3 _currentVelocity;

    public void Initialize(BattleCameraTransform cameraTransform)
    {
        transform.position = cameraTransform.Position.ToVector3();
        transform.rotation = cameraTransform.Rotation.ToQuaternion();
    }

    public void OnUpdate(BattleCameraTransform cameraTransform, in BattleFrame frame)
    {
        var position = transform.position;
        var targetPosition = cameraTransform.Position.ToVector3();
        var newPosition = Vector3.SmoothDamp(position, targetPosition, ref _currentVelocity, POSITION_SMOOTH_TIME);
        transform.position = newPosition;

        var newRotation = Quaternion.RotateTowards(transform.rotation, cameraTransform.Rotation.ToQuaternion(), MAX_ROTATION_DEGREES * (float)frame.DeltaTime);
        transform.rotation = newRotation;
    }
}
