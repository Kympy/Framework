#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DragonGate
{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public sealed class LayerAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(position, label.text, "Use Layer with int.");
                return;
            }

            property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        }
    }
}
#endif
