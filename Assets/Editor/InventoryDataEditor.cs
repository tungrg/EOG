#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InventoryData))]
public class InventoryDataEditor : UnityEditor.Editor // Rõ ràng dùng UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        InventoryData inventory = (InventoryData)target;
        if (GUILayout.Button("Reset Inventory"))
        {
            inventory.Clear();
            EditorUtility.SetDirty(inventory);
            Debug.Log("[InventoryDataEditor] Inventory reset.");
        }
    }
}
#endif