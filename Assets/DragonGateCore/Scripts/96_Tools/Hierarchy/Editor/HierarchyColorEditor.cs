using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DragonGate
{
    [InitializeOnLoad]
    public static class HierarchyColorEditor
    {
        private static readonly HashSet<EntityId> _selectedEntityIds = new ();
    
        static HierarchyColorEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItemDraw;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemDraw;

            Selection.selectionChanged -= UpdateSelections;
            Selection.selectionChanged += UpdateSelections;
        }

        private static void UpdateSelections()
        {
            _selectedEntityIds.Clear();
            var current = Selection.entityIds;
            for (int i = 0; i < current.Length; i++)
            {
                _selectedEntityIds.Add(current[i]);
            }
        }

        private const string GameObjectIcon = "GameObject Icon";
        private static void OnHierarchyItemDraw(int entityId, Rect selectedRect)
        {
            GameObject obj = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (obj == null) return;

            obj.TryGetComponent(out HierarchyColor colorComponent);
            if (colorComponent == null) return;
            
            bool isSelected = _selectedEntityIds.Contains(entityId);

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
