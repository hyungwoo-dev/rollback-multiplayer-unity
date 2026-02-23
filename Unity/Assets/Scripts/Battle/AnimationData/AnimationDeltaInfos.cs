using FixedMathSharp;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AnimationDeltaInfo
{
    public string StateName;
    public bool IsLooping;
    public List<Vector3d> DeltaPositions;
    public List<FixedQuaternion> DeltaRotations;
}

[Serializable]
public class AnimationDeltaInfos : ISerializationCallbackReceiver
{
    public static void EnsureInstance()
    {
        if (Shared != null)
        {
            return;
        }

        var json = Resources.Load<TextAsset>(FILE_NAME_WITHOUT_EXTENSIONS).text;
        Shared = JsonUtility.FromJson<AnimationDeltaInfos>(json);
    }

    public static AnimationDeltaInfos Shared { get; private set; }

    public const string FILE_NAME_WITHOUT_EXTENSIONS = "animation_delta_infos";
    public const string FILE_NAME = FILE_NAME_WITHOUT_EXTENSIONS + ".bytes";

    [SerializeField]
    private List<AnimationDeltaInfo> DeltaInfos;

    [NonSerialized]
    private Dictionary<string, AnimationDeltaInfo> _deltaInfoDictionary = new();

    public AnimationDeltaInfos(List<AnimationDeltaInfo> deltaInfos)
    {
        DeltaInfos = deltaInfos;
    }

    public (Vector3d DeltaPosition, FixedQuaternion DeltaRotation) GetDeltas(string stateName, int frame)
    {
        var deltaInfo = _deltaInfoDictionary[stateName];
        var count = deltaInfo.DeltaPositions.Count;
        if (frame >= count && !deltaInfo.IsLooping)
        {
            return (Vector3d.Zero, FixedQuaternion.Identity);
        }

        var index = frame % count;
        var deltaPosition = deltaInfo.DeltaPositions[index];
        var deltaRotation = deltaInfo.DeltaRotations[index];
        return (deltaPosition, deltaRotation);
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {

    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        _deltaInfoDictionary = new Dictionary<string, AnimationDeltaInfo>();
        foreach (var deltaInfo in DeltaInfos)
        {
            _deltaInfoDictionary.Add(deltaInfo.StateName, deltaInfo);
        }
    }
}
