using FixedMathSharp;

[ManagedState(typeof(BattleWorld))]
public partial class BattleCameraTransform
{
    [ManagedStateIgnore]
    private BattleWorld World { get; set; }

    public Vector3d Position { get; private set; }
    public FixedQuaternion Rotation { get; private set; }

    public Vector3d NextPosition { get; private set; }
    public FixedQuaternion NextRotation { get; private set; }

    public Fixed4x4 FixedTransform => Fixed4x4.TranslateRotateScale(Position, Rotation, Vector3d.One);

    public void Initialize(BattleWorld world, Vector3d position, FixedQuaternion rotation)
    {
        World = world;

        SetPositionAndRotation(position, rotation);
        NextPosition = position;
        NextRotation = rotation;
    }

    public void AdvanceFrame(in BattleFrame frame)
    {
        Fixed64 INTERPOLATE_SCALE = new Fixed64(6.0d);

        var (targetPosition, targetRotation) = World.GetCameraTargetPositionAndRotation(FixedTransform);

        var nextPosition = Vector3d.Lerp(Position, targetPosition, frame.DeltaTime * INTERPOLATE_SCALE);
        var nextRotation = FixedQuaternion.Slerp(Rotation, targetRotation, frame.DeltaTime * INTERPOLATE_SCALE);
        UpdatePositionAndRotation(nextPosition, nextRotation);
    }

    public int GetCameraHash()
    {
        long longHash = long.MaxValue;
        longHash ^= Position.x.m_rawValue;
        longHash ^= Position.y.m_rawValue;
        longHash ^= Position.z.m_rawValue;
        longHash ^= Rotation.x.m_rawValue;
        longHash ^= Rotation.y.m_rawValue;
        longHash ^= Rotation.z.m_rawValue;
        longHash ^= Rotation.w.m_rawValue;

        longHash ^= NextPosition.x.m_rawValue;
        longHash ^= NextPosition.y.m_rawValue;
        longHash ^= NextPosition.z.m_rawValue;
        longHash ^= NextRotation.x.m_rawValue;
        longHash ^= NextRotation.y.m_rawValue;
        longHash ^= NextRotation.z.m_rawValue;
        longHash ^= NextRotation.w.m_rawValue;

        var uinthash = (uint)(longHash % uint.MaxValue);
        var intHash = (int)uinthash;
        return intHash;
    }

    private void SetPositionAndRotation(Vector3d position, FixedQuaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    private void UpdatePositionAndRotation(Vector3d position, FixedQuaternion rotation)
    {
        SetPositionAndRotation(NextPosition, NextRotation);
        NextPosition = position;
        NextRotation = rotation;
    }
}
