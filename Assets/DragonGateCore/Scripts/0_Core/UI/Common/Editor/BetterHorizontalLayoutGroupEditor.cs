using UnityEditor;
using UnityEditor.UI;

namespace DragonGate
{
    [CustomEditor(typeof(BetterHorizontalLayoutGroup))]
    public class BetterHorizontalLayoutGroupEditor : HorizontalOrVerticalLayoutGroupEditor
    {
        private SerializedProperty _yDistance;
        private SerializedProperty _reverse;

        protected override void OnEnable()
        {
            base.OnEnable();
            _yDistance = serializedObject.FindProperty("_yDistance");
            _reverse = serializedObject.FindProperty("_reverse");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_yDistance);
            EditorGUILayout.PropertyField(_reverse);
            serializedObject.ApplyModifiedProperties();
        }
    }
}