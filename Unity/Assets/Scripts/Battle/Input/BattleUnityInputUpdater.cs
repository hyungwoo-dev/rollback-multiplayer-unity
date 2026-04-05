using System;
using UnityEngine;

public class BattleUnityInputUpdater : MonoBehaviour
{
    public static BattleUnityInputUpdater Create()
    {
        var gameObject = new GameObject(nameof(BattleUnityInputUpdater));
        var instance = gameObject.AddComponent<BattleUnityInputUpdater>();
        return instance;
    }

    public Action OnUpdate;

    private void Update()
    {
        OnUpdate?.Invoke();
    }
}
