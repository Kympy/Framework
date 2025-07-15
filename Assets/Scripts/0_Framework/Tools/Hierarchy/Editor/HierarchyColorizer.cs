using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Framework
{
    [InitializeOnLoad]
    public static class HierarchyColorizer
    {
        private static HashSet<int> selectedInstanceIds = new HashSet<int>();
    
        static HierarchyColorizer()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItemDraw;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemDraw;

            Selection.selectionChanged -= UpdateSelections;
            Selection.selectionChanged += UpdateSelections;
        }

        private static void UpdateSelections()
        {
            selectedInstanceIds.Clear();
            var current = Selection.instanceIDs;
            for (int i = 0; i < current.Length; i++)
            {
                selectedInstanceIds.Add(current[i]);
            }
        }

        private const string GameObjectIcon = "GameObject Icon";
        private static void OnHierarchyItemDraw(int instanceId, Rect selectedRect)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (obj == null) return;

            obj.TryGetComponent(out HierarchyColor colorComponent);
            if (colorComponent == null) return;
            
            bool isSelected = selectedInstanceIds.Contains(instanceId);

            Color bgColor = isSelected ? GUI.skin.settings.selectionColor : colorComponent.BackgroundColor;
            Color textColor = isSelected ? Color.white : colorComponent.TextColor;

            EditorGUI.DrawRect(selectedRect, bgColor);
            
            Color originColor = GUI.color;
            GUI.color = textColor;
            
            GUIContent iconContent = EditorGUIUtility.IconContent(GameObjectIcon);
            EditorGUI.LabelField(new Rect(selectedRect.x - 1, selectedRect.y - 1, 18, 18), iconContent);

            EditorGUI.LabelField(new Rect(selectedRect.x + 17, selectedRect.y, selectedRect.width - 17, selectedRect.height), obj.name);
            
            GUI.color = originColor;
        }
    }
}
