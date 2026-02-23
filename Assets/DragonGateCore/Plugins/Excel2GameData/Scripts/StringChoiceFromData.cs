using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace DragonGate
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class StringChoiceFromData : PropertyAttribute
    {

    }

    [CustomPropertyDrawer(typeof(StringChoiceFromData))]
    public class StringChoicesFromDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect positionRectangle, SerializedProperty serializedProperty, GUIContent label)
        {
            if (serializedProperty.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(positionRectangle, serializedProperty, label);
                return;
            }

            EditorGUI.BeginProperty(positionRectangle, label, serializedProperty);

            var currentKey = serializedProperty.longValue;
            string displayName;
            CheckStringData();
            // string 사용 방식이 unity localization으로 변경됨에 따라 동작 x
            displayName = currentKey != 0 ? currentKey.ToString() : "<None>";

            if (EditorGUI.DropdownButton(
                    positionRectangle,
                    new GUIContent(displayName),
                    FocusType.Keyboard))
            {
                var options = GetOptions((StringChoiceFromData)attribute);
                var dropdown = new StringDataDropdown(
                    new AdvancedDropdownState(),
                    options,
                    selectedKey =>
                    {
                        serializedProperty.longValue = selectedKey;
                        serializedProperty.serializedObject.ApplyModifiedProperties();
                        GUI.changed = true;
                    });

                dropdown.Show(positionRectangle);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private static IReadOnlyList<long> GetOptions(StringChoiceFromData attributeInstance)
        {
            CheckStringData();
            // var data = GameDataManager.Editor.StringDataDictionary;
            // if (data == null || data.Count == 0)
            // {
            //     return Array.Empty<long>();
            // }
            // return data.Keys.ToArray();
            return null;
        }

        private static int IndexOf(IReadOnlyList<long> list, long value)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == value)
                    return i;
            return -1;
        }

        private static void CheckStringData()
        {
            // var data = GameDataManager.Editor.StringDataDictionary;
            // if (data == null || data.Count == 0)
            // {
            //     GameDataManager.Editor.LoadStringData();
            // }
        }
    }
}
