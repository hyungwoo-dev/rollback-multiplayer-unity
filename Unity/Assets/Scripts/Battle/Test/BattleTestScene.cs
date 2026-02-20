using UnityEngine;

public class BattleTestScene : MonoBehaviour
{
    [SerializeField]
    private BattlePlayMode _playMode;

    public void LoadBattleScene()
    {
        StartCoroutine(BattleScene.CoLoad(new BattleSceneLoadContext()
        {
            PlayMode = _playMode
        }));
    }
}
