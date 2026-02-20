using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleTestScene))]
public class BattleTestSceneInspector : Editor
{
    private BattleTestScene _target;

    private void OnEnable()
    {
        _target = target as BattleTestScene;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Load"))
        {
            _target.LoadBattleScene();
        }
    }
}
