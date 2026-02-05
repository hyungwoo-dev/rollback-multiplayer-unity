using log4net.Util;
using UnityEngine;

public class BattleCamera : MonoBehaviour
{
    [SerializeField]
    private Vector3 CurrentVelocity;

    [SerializeField]
    private float SmoothTime = 1.0f;
    private BattleWorld World { get; set; }

    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; }

    public Vector3 NextPosition { get; private set; }
    public Quaternion NextRotation { get; private set; }

    private float InterpolatingTime { get; set; }

    public void Initialize(BattleWorld world)
    {
        World = world;

        var (targetPosition, targetRotation) = World.GetCameraTargetPositionAndRotation(transform);

        SetPositionAndRotation(targetPosition, targetRotation);

        NextPosition = targetPosition;
        NextRotation = targetRotation;
    }

    public void OnFixedUpdate(in BattleFrame frame)
    {
        const float INTERPOLATE_SCALE = 6.0f;
        InterpolatingTime = 0.0f;

        var (targetPosition, targetRotation) = World.GetCameraTargetPositionAndRotation(transform);

        var nextPosition = Vector3.Lerp(transform.position, targetPosition, frame.DeltaTime * INTERPOLATE_SCALE);
        var nextRotation = Quaternion.Slerp(transform.rotation, targetRotation, frame.DeltaTime * INTERPOLATE_SCALE);
        UpdatePositionAndRotation(nextPosition, nextRotation);
    }

    public void Interpolate(in BattleFrame frame)
    {
        InterpolatingTime += frame.DeltaTime;

        var t = Mathf.Clamp01(InterpolatingTime * frame.InverseFixeedDeltaTime);
        transform.position = Vector3.Lerp(Position, NextPosition, t);
        transform.rotation = Quaternion.Slerp(Rotation, NextRotation, t);
    }

    private void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;

        transform.position = position;
        transform.rotation = rotation;
    }

    private void UpdatePositionAndRotation(Vector3 position, Quaternion rotation)
    {
        SetPositionAndRotation(NextPosition, NextRotation);
        NextPosition = position;
        NextRotation = rotation;
    }
}
