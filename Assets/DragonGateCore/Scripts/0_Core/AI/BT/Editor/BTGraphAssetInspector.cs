using DragonGate;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// BTGraphAsset 인스펙터 커스텀 UI.
/// 더블클릭 시 BT Graph Editor 창이 열린다.
/// </summary>
[CustomEditor(typeof(BTGraphAsset))]
public class BTGraphAssetInspector : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("BT Graph Editor 열기", GUILayout.Height(30)))
        {
            BTGraphEditorWindow.Open(target as BTGraphAsset);
        }

        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }

    [OnOpenAsset]
    public static bool OnOpenAsset(int entityId, int line)
    {
        var asset = EditorUtility.EntityIdToObject(entityId) as BTGraphAsset;
        if (asset == null) return false;

        BTGraphEditorWindow.Open(asset);
        return true;
    }
}