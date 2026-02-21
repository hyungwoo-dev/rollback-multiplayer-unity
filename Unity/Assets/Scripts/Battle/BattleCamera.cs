using FixedMathSharp;
using UnityEngine;

public class BattleCamera : MonoBehaviour
{
    [SerializeField]
    private Vector3d CurrentVelocity;

    private BattleWorld World { get; set; }

    public Vector3d Position { get; private set; }
    public FixedQuaternion Rotation { get; private set; }

    public Vector3d NextPosition { get; private set; }
    public FixedQuaternion NextRotation { get; private set; }

    private Fixed64 InterpolatingTime { get; set; }

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
        Fixed64 INTERPOLATE_SCALE = new Fixed64(6.0d);
        InterpolatingTime = Fixed64.Zero;

        var (targetPosition, targetRotation) = World.GetCameraTargetPositionAndRotation(transform);

        var nextPosition = Vector3d.Lerp(transform.position.ToVector3d(), targetPosition, frame.DeltaTime * INTERPOLATE_SCALE);
        var nextRotation = FixedQuaternion.Slerp(transform.rotation.ToFixedQuaternion(), targetRotation, frame.DeltaTime * INTERPOLATE_SCALE);
        UpdatePositionAndRotation(nextPosition, nextRotation);
    }

    public void Interpolate(in BattleFrame frame)
    {
        InterpolatingTime += frame.DeltaTime;

        var t = (InterpolatingTime / frame.FixedDeltaTime).Clamp01();
        transform.position = Vector3d.Lerp(Position, NextPosition, t).ToVector3();
        transform.rotation = FixedQuaternion.Slerp(Rotation, NextRotation, t).ToQuaternion();
    }

    private void SetPositionAndRotation(Vector3d position, FixedQuaternion rotation)
    {
        Position = position;
        Rotation = rotation;

        transform.position = position.ToVector3();
        transform.rotation = rotation.ToQuaternion();
    }

    private void UpdatePositionAndRotation(Vector3d position, FixedQuaternion rotation)
    {
        SetPositionAndRotation(NextPosition, NextRotation);
        NextPosition = position;
        NextRotation = rotation;
    }
}
