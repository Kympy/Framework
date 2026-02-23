using System;
using UnityEditor;
using UnityEngine;

namespace DragonGate
{
    [CustomEditor(typeof(SoftImage))]
    [CanEditMultipleObjects]
    public class SoftImageEditor : UnityEditor.UI.ImageEditor
    {
        private SerializedProperty _spriteReference;
        private SerializedProperty _preserveAspect;

        protected override void OnEnable()
        {
            base.OnEnable();
            _spriteReference = serializedObject.FindProperty("_spriteReference");
            _preserveAspect = serializedObject.FindProperty("m_PreserveAspect");
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_spriteReference, new GUIContent("Sprite Reference"));
            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();
            EditorGUILayout.PropertyField(_preserveAspect, new GUIContent("Preserve Aspect"));
            TypeGUI();
            NativeSizeButtonGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}