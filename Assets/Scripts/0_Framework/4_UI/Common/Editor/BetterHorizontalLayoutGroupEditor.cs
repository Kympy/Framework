using UnityEditor;
using UnityEditor.UI;

namespace Framework
{
    [CustomEditor(typeof(BetterHorizontalLayoutGroup))]
    public class BetterHorizontalLayoutGroupEditor : HorizontalOrVerticalLayoutGroupEditor
    {
        private SerializedProperty _yDistance;

        protected override void OnEnable()
        {
            base.OnEnable();
            _yDistance = serializedObject.FindProperty("_yDistance");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_yDistance);
            serializedObject.ApplyModifiedProperties();
        }
    }
}