using UnityEditor;
using UnityEditor.UI;

namespace DragonGate
{
    [CustomEditor(typeof(BetterVerticalLayoutGroup))]
    public class BetterVerticalLayoutGroupEditor : HorizontalOrVerticalLayoutGroupEditor
    {
        private SerializedProperty _xDistance;
        private SerializedProperty _reverse;

        protected override void OnEnable()
        {
            base.OnEnable();
            _xDistance = serializedObject.FindProperty("_xDistance");
            _reverse = serializedObject.FindProperty("_reverse");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_xDistance);
            EditorGUILayout.PropertyField(_reverse);
            serializedObject.ApplyModifiedProperties();
        }
    }
}