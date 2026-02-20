using UnityEngine;

public class BattleStartScene : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(BattleScene.CoLoad(new BattleSceneLoadContext()
        {
            PlayMode = BattlePlayMode.Multiplay,
        }));
    }
}
