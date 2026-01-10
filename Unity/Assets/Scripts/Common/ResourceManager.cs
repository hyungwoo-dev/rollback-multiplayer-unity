using UnityEngine;

public class ResourceManager
{
    public static GameObject InstantiateUnit()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Unit");
        var instance = GameObject.Instantiate(prefab);
        return instance;
    }

    public static GameObject InstantiateUnitCollider()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Unit_Collider");
        var instance = GameObject.Instantiate(prefab);
        return instance;
    }
}
