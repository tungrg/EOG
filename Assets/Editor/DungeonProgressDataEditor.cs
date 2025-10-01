#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DungeonProgressData))]
public class DungeonProgressDataEditor : UnityEditor.Editor // Rõ ràng dùng UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DungeonProgressData progress = (DungeonProgressData)target;
        if (GUILayout.Button("Reset Progress"))
        {
            progress.ResetProgress();
            EditorUtility.SetDirty(progress);
            Debug.Log("[DungeonProgressDataEditor] Progress reset.");
        }
    }
}
#endif